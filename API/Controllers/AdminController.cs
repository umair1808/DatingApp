using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUserWithRoles()
        {
            var users = await _userManager.Users
                        .Include(r => r.UserRoles)
                        .ThenInclude(r => r.Role)
                        .OrderBy(u => u.UserName)
                        .Select(u => new
                        {
                            u.Id,
                            Username = u.UserName,
                            Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
                        })
                        .ToListAsync();

            return Ok(users);

            // Instructor notes: Pagination can be used here as well just like it is used in user controller
        }


        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
        {
            var selectedRoles = roles.Split(",");

            var user = await _userManager.FindByNameAsync(username);

            if (user == null) return NotFound("Could not find user");

            var userRoles = await _userManager.GetRolesAsync(user);

            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded) return BadRequest("Failed to add to roles");

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded) return BadRequest("Failed to remove from roles");

            return Ok(await _userManager.GetRolesAsync(user));
        }


        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public async Task<ActionResult> GetPhotosForModeration()
        {
            var photos = await _unitOfWork.UserRepository.GetUnapprovedPhotos();

            return Ok(_mapper.Map<IEnumerable<PhotoDto>>(photos));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPut("approve-photo/{id}")]
        public async Task<ActionResult> ApprovePhoto(int id)
        {
            var photo = _unitOfWork.UserRepository.GetPhotoByIdAsync(id).Result;
            if (photo != null)
                {
                    photo.IsApproved = true;

                    //find the user whose photo is this
                    var user = await _userManager.FindByIdAsync(photo.AppUserId.ToString());

                    //find out if this user has main photo set
                    var hasMainPhoto = user.Photos.Any(p => p.IsMain);
                    if (!hasMainPhoto) photo.IsMain = true;

                    return Ok(_mapper.Map<PhotoDto>(photo));
                }

            return NotFound();
        }
    }
}