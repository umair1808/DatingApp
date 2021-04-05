using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class LikesController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly ILikesRepository _likesRepository;
        public LikesController(IUserRepository userRepository, ILikesRepository likesRepository)
        {
            _likesRepository = likesRepository;
            _userRepository = userRepository;
        }

        [HttpPost("{username}")] //username that is going to be liked by logged in user
        public async Task<ActionResult> AddLike(string username){
            
            var sourceUserId = User.GetUserId(); //ClaimsPrincipal extension
            var sourceUser = await _likesRepository.GetUserWithLikes(sourceUserId); //return user with all of its likes included
            var likedUser = await _userRepository.GetUserByUsernameAsync(username);

            if(likedUser == null) return NotFound();

            if(sourceUser.UserName == username) return BadRequest("You cannot like yourself");

            //check to see if the user is already liked by logged in user
            var userLike = await _likesRepository.GetUserLike(sourceUserId, likedUser.Id);
            if(userLike != null) return BadRequest("You already like this user");
            //TODO: this functionality can be extented to unlke a user if it already liked

            userLike = new UserLike
            {
                SourceUserId = sourceUserId,
                LikedUserId = likedUser.Id
            };

            sourceUser.LikedUsers.Add(userLike);

            if(await _userRepository.SaveAllAsync()) return Ok(); //TODO: _likeRepository should have its own save

            return BadRequest("Failed to like user");

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDto>>> GetUserLikes([FromQuery] LikesParams likesParams){

            likesParams.UserId = User.GetUserId();
            var users = await _likesRepository.GetUserLikes(likesParams);

            //Adding the Pagination Information in the response to be used in the next request
            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }
    }
}