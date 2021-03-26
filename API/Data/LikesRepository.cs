using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class LikesRepository : ILikesRepository
    {
        private readonly DataContext _context;
        public LikesRepository(DataContext context)
        {
            _context = context;
        }

        /**
        * Getting individual like of this (source user)
        **/
        public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId)
        {
            return await _context.Likes.FindAsync(sourceUserId, likedUserId);
        }

        /**
        * Getting a user with all of his likes 
        **/
        public async Task<AppUser> GetUserWithLikes(int userId)
        {
            return await _context.Users
            .Include(l => l.LikedUsers)
            .FirstOrDefaultAsync(x => x.Id == userId); 
        }

        /**
        * Getting list of users this user has liked 
          This method can also get get the list of users that have liked this user
        **/
        public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams)
        {
            var users = _context.Users.OrderBy(u => u.Username).AsQueryable();
            var likes = _context.Likes.AsQueryable();

            if(likesParams.Predicate == "liked"){
                users = likes.Where(l => l.SourceUserId == likesParams.UserId).Select(l => l.LikedUser);
            }
            else if(likesParams.Predicate == "likedBy"){
                users = likes.Where(l => l.LikedUserId == likesParams.UserId).Select(l => l.SourceUser);
            }

            var query = users.Select(u => new LikeDto {
                Id = u.Id,
                Username = u.Username,
                Age = u.DateOfBirth.CalculateAge(),
                KnownAs = u.KnownAs,
                PhotoUrl = u.Photos.FirstOrDefault(p => p.IsMain).Url,
                City = u.City
            }).AsQueryable();

            return await PagedList<LikeDto>.CreateAsync(query, likesParams.PageNumber, likesParams.PageSize);
        }
    }
}