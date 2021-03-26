using System.Security.Claims;

namespace API.Extensions
{
    public static class ClaimsPrincipleExtensions
    {
        public static string GetUsername(this ClaimsPrincipal user){
            return user.FindFirst(ClaimTypes.Name)?.Value; //.Name is like JwtRegisteredClaimNames.UniqueName
        }

        public static int GetUserId(this ClaimsPrincipal user){
            return int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value); //.NameIdentifier  is like JwtRegisteredClaimNames.NameId

            //int.Parse exception not handled
        }
    }
}