using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebChat.Entities;
using WebChat.Services;

namespace WebChat.Controllers
{
    //   GET  /api/conversations              — the caller's conversations
    //   GET  /api/conversations/{id}/messages — history, participants only
    //   POST /api/conversations/direct        — open (or reuse) a DM
    [Authorize]
    [ApiController]
    [Route("api/conversations")]
    public class ConversationsController : ControllerBase
    {
        private readonly ConversationService _conversationService;
        private readonly UserManager<AppUser> _userManager;

        public ConversationsController(
            ConversationService conversationService,
            UserManager<AppUser> userManager)
        {
            _conversationService = conversationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var meId = _userManager.GetUserId(User)!;
            return Ok(await _conversationService.GetConversationsAsync(meId));
        }

        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetMessages(string id)
        {
            var meId = _userManager.GetUserId(User)!;

            if (!await _conversationService.ConversationExistsAsync(id)) return NotFound();

            // Signed in is not the same as allowed to read this.
            if (!await _conversationService.IsParticipantAsync(id, meId)) return Forbid();

            return Ok(await _conversationService.GetMessagesAsync(id));
        }

        public record DirectRequest(string UserId);

        [HttpPost("direct")]
        public async Task<IActionResult> OpenDirect([FromBody] DirectRequest request)
        {
            var meId = _userManager.GetUserId(User)!;

            if (string.IsNullOrWhiteSpace(request.UserId)) return BadRequest("A user id is required.");
            if (request.UserId == meId) return BadRequest("Cannot open a direct message with yourself.");
            if (await _userManager.FindByIdAsync(request.UserId) is null) return NotFound("No such user.");

            var conversationId = await _conversationService.GetOrCreateDirectAsync(meId, request.UserId);
            var conversation = await _conversationService.GetConversationForUserAsync(conversationId, meId);

            return Ok(conversation);
        }
    }
}
