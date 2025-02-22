﻿using exercise.wwwapi.Helpers;
using exercise.wwwapi.Models;
using exercise.wwwapi.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using System.Diagnostics.Eventing.Reader;
using System.Security.Claims;


namespace exercise.wwwapi.Endpoints
{
    public static class SecureApi
    {
        public static void ConfigureSecureApi(this WebApplication app)
        {
            app.MapGet("message", GetMessage);
            app.MapGet("posts", GetPosts);
            app.MapPost("newpost", MakePost);
            app.MapPut("updatepost", UpdatePost);
            app.MapGet("postswithcomments", GetPostsWithComments);
            app.MapPost("Post{postId}/comment{content}", CommentOnPost);

        }
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public static async Task<IResult> CommentOnPost(IRepository<Post> postRepo, IRepository<Comment> commentRepo, int postId, ClaimsPrincipal user, string content )
        {
            Post post = postRepo.GetById(postId);
            Comment comment = new Comment
            {
                userId = ClaimsPrincipalHelper.UserRealId(user),
                postId = postId,
                content = content
            };
            commentRepo.Insert(comment);    
            post.comments.Add(comment);
            commentRepo.Save();
            postRepo.Save();
            return TypedResults.Ok(new CommentDTO(comment));
        }


        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public static async Task<IResult> GetPostsWithComments(IRepository<Post> repo)
        {
            List<Post> posts = repo.GetAll().ToList();
            List<PostDTO> postDTOs = new List<PostDTO>();
            posts.ForEach(x => postDTOs.Add(new PostDTO(x)));
            return TypedResults.Ok(postDTOs);
        }

        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        private static async Task<IResult> GetMessage(IRepository<User> service, ClaimsPrincipal user, ILogger logger)
        {
            logger.LogDebug(new string('*', 1000));
            return TypedResults.Ok(new { LoggedIn = true, UserId = user.UserRealId().ToString(), Email = $"{user.Email()}", Message = "Pulled the userid and email out of the claims" });
        }
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        private static async Task<IResult> GetPosts(IRepository<Post> service) 
        {
            List<PostDTO> posts = new List<PostDTO>();
            service.GetAll().ToList().ForEach(post => posts.Add(new PostDTO(post)));// convert all posts to postDTO

            return TypedResults.Ok(posts);
        }
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        private static async Task<IResult> MakePost(IRepository<Post> service,ClaimsPrincipal user,  string title, string content )
        {
            Post post = new Post { postTitle = title, content = content, userId=ClaimsPrincipalHelper.UserRealId(user) };
            service.Insert(post);
            service.Save();
            return TypedResults.Ok(new PostDTO(post));
        }
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
       
        private static async Task<IResult> UpdatePost(IRepository<Post> service, int postId, ClaimsPrincipal user,  string title, string content)
        {

            Post post = service.GetById(postId);
            if (post.userId != ClaimsPrincipalHelper.UserRealId(user))
            {
                return TypedResults.Unauthorized();
            }

            else
            {
                post.postTitle = title;
                post.content = content;
                service.Save();
                return TypedResults.Ok();
            }

        }
    }
}
