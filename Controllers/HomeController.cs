using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CollaborativeWhiteboard.Models;

namespace CollaborativeWhiteboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create a new session
        [HttpPost("CreateSession")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { success = false, error = "Session name cannot be empty" });

            try
            {
                var session = new WhiteboardSession
                {
                    Id = Guid.NewGuid().ToString(), // string ID
                    Name = request.Name,
                };

                _context.WhiteboardSessions.Add(session);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, sessionId = session.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // Load a session with drawing actions
        [HttpGet("LoadSession/{sessionId}")]
        public async Task<IActionResult> LoadSession(string sessionId)
        {
            try
            {
                var session = await _context.WhiteboardSessions
                    .Include(s => s.DrawingActions)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                    return NotFound(new { success = false, error = "Session not found" });

                var drawingActions = session.DrawingActions
                    .OrderBy(a => a.Timestamp)
                    .Select(a => new
                    {
                        a.ActionType,
                        a.StartX,
                        a.StartY,
                        a.EndX,
                        a.EndY,
                        a.Color,
                        a.LineWidth,
                        a.UserId
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    session = new
                    {
                        session.Id,
                        session.Name,
                        session.CreatedDate,
                        session.LastModified
                    },
                    drawingActions
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // Get all sessions
        [HttpGet("GetSessions")]
        public async Task<IActionResult> GetSessions()
        {
            try
            {
                var sessions = await _context.WhiteboardSessions
                    .OrderByDescending(s => s.LastModified)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.CreatedDate,
                        s.LastModified
                    })
                    .ToListAsync();

                return Ok(new { success = true, sessions });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // Save a drawing action
        [HttpPost("SaveDrawingAction")]
        public async Task<IActionResult> SaveDrawingAction([FromBody] DrawingActionRequest request)
        {
            try
            {
                // sessionId is string now
                var session = await _context.WhiteboardSessions
                    .Include(s => s.DrawingActions)
                    .FirstOrDefaultAsync(s => s.Id == request.SessionId);

                if (session == null)
                    return NotFound(new { success = false, error = "Session not found" });

                var action = new DrawingAction
                {
                    Id = Guid.NewGuid().ToString(), // string ID
                    SessionId = request.SessionId,   // string FK
                    ActionType = request.ActionType,
                    StartX = request.StartX,
                    StartY = request.StartY,
                    EndX = request.EndX,
                    EndY = request.EndY,
                    Color = request.Color,
                    LineWidth = request.LineWidth,
                    UserId = request.UserId,
                    Timestamp = DateTime.UtcNow
                };

                _context.DrawingActions.Add(action);
                session.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, actionId = action.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }

    // DTOs
    public class CreateSessionRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class DrawingActionRequest
    {
        public string SessionId { get; set; } = string.Empty; // string now
        public string ActionType { get; set; } = string.Empty;
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float EndX { get; set; }
        public float EndY { get; set; }
        public string Color { get; set; } = "#000000";
        public float LineWidth { get; set; }
        public string UserId { get; set; } = "anonymous";
    }
}
