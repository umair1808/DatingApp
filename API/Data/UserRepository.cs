using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public UserRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _context.Users
            .Include(p => p.Photos)
            .ToListAsync();
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == username);
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public void Update(AppUser user)
        {
            _context.Entry(user).State = EntityState.Modified;
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            //TODO: Bug where current logged in user is sometimes also sent back in response json

            // var query = _context.Users.Where(u => u.Username != userParams.CurrentUsername).AsQueryable();
            // query = query.Where(u => u.Gender == userParams.Gender).AsQueryable();

            // var query = _context.Users.ProjectTo<MemberDto>(_mapper.ConfigurationProvider).AsNoTracking();

            var minDob = DateTime.Today.AddYears(-userParams.MaxAge-1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

            var users = _context.Users.Where(u => u.UserName.ToLower() != userParams.CurrentUsername.ToLower());
            users = users.Where(u => u.Gender == userParams.Gender);
            users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

            users = userParams.OrderBy switch {
                "created" => users.OrderByDescending(u => u.Created),
                _ => users.OrderByDescending(u => u.LastActive)
            };

            var query = users.ProjectTo<MemberDto>(_mapper.ConfigurationProvider).AsNoTracking();

            return await PagedList<MemberDto>.CreateAsync(query, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<MemberDto> GetMemberAsync(string username, bool ignoreApprovedPhotosFilter = false)
        {
            var query = _context.Users
            .Where(x => x.UserName == username)
            .ProjectTo<MemberDto>(_mapper.ConfigurationProvider);

            if(ignoreApprovedPhotosFilter)
            {
                 query = query.IgnoreQueryFilters();
            }
            return await query.SingleOrDefaultAsync();
        }

        public async Task<string> GetUserGender(string username)
        {
            return await _context.Users
                    .Where(x => x.UserName == username)
                    .Select(x => x.Gender).FirstOrDefaultAsync();
        }

        public async Task<Photo> GetPhotoByIdAsync(int id)
        {
            return await _context.Photos.Where(p => p.Id == id)
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<Photo>> GetUnapprovedPhotos()
        {
            return await _context.Photos
            .Where(p => p.IsApproved != true)
            .IgnoreQueryFilters()
            .ToListAsync();
        }
    }
}