 using Microsoft.AspNetCore.SignalR;
using CollaborativeWhiteboard.Models;

namespace CollaborativeWhiteboard.Hubs
{
    /// <summary>
    /// SignalR Hub for handling real-time whiteboard communication
    /// This is the core of real-time functionality - it receives messages from clients
    /// and broadcasts them to all other connected clients in the same session
    /// </summary>
    public class WhiteboardHub : Hub
    {
        private readonly ApplicationDbContext _context;
        
        public WhiteboardHub(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Called when a client connects to the hub
        /// We can use this to perform initialization tasks
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }
        
        /// <summary>
        /// Called when a client disconnects
        /// Good place for cleanup operations
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
        
        /// <summary>
        /// Join a specific whiteboard session
        /// SignalR Groups allow us to broadcast messages only to clients in the same session
        /// </summary>
        public async Task JoinSession(string sessionId, string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
            
            // Notify other users in the session that someone joined
            await Clients.Group($"Session_{sessionId}")
                .SendAsync("UserJoined", new { UserId = userId, ConnectionId = Context.ConnectionId });
                
            Console.WriteLine($"User {userId} joined session {sessionId}");
        }
        
        /// <summary>
        /// Leave a whiteboard session
        /// </summary>
        public async Task LeaveSession(string sessionId, string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
            
            // Notify other users that someone left
            await Clients.Group($"Session_{sessionId}")
                .SendAsync("UserLeft", new { UserId = userId });
                
            Console.WriteLine($"User {userId} left session {sessionId}");
        }
        
        /// <summary>
        /// Handle drawing actions from clients
        /// This is where the real-time magic happens:
        /// 1. Receive drawing data from one client
        /// 2. Save it to database for persistence
        /// 3. Broadcast it to all other clients in the same session
        /// </summary>
        public async Task SendDrawingAction(object drawingData)
        {
            try
            {
                // Parse the drawing data (sent as anonymous object from JavaScript)
                var data = System.Text.Json.JsonSerializer.Serialize(drawingData);
                var action = System.Text.Json.JsonSerializer.Deserialize<DrawingActionDto>(data);
                
                if (action == null) return;
                
                // Save to database for persistence
                var dbAction = new DrawingAction
                {
                    SessionId = action.SessionId,
                    UserId = action.UserId,
                    ActionType = action.ActionType,
                    StartX = action.StartX,
                    StartY = action.StartY,
                    EndX = action.EndX,
                    EndY = action.EndY,
                    Color = action.Color,
                    LineWidth = action.LineWidth,
                    Timestamp = DateTime.UtcNow
                };
                
                _context.DrawingActions.Add(dbAction);
                await _context.SaveChangesAsync();
                
                // Broadcast to all clients in this session (EXCEPT the sender)
                // This prevents the drawing from appearing twice on the sender's canvas
                await Clients.GroupExcept($"Session_{action.SessionId}", Context.ConnectionId)
                    .SendAsync("ReceiveDrawingAction", action);
                    
                Console.WriteLine($"Drawing action broadcasted for session {action.SessionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing drawing action: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handle clear board action
        /// When one user clears the board, it should be cleared for everyone
        /// </summary>
        public async Task ClearBoard(string sessionId, string userId)
        {
            try
            {
                // Save clear action to database
                var clearAction = new DrawingAction
                {
                    SessionId = sessionId,
                    UserId = userId,
                    ActionType = "clear",
                    StartX = 0,
                    StartY = 0,
                    EndX = 0,
                    EndY = 0,
                    Color = "#000000",
                    LineWidth = 0,
                    Timestamp = DateTime.UtcNow
                };
                
                _context.DrawingActions.Add(clearAction);
                await _context.SaveChangesAsync();
                
                // Broadcast clear action to all clients in session
                await Clients.Group($"Session_{sessionId}")
                    .SendAsync("ClearBoard", new { UserId = userId });
                    
                Console.WriteLine($"Board cleared for session {sessionId} by user {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing board: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// DTO for drawing action data transfer
    /// This matches the structure of data sent from JavaScript
    /// </summary>
    public class DrawingActionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float EndX { get; set; }
        public float EndY { get; set; }
        public string Color { get; set; } = "#000000";
        public float LineWidth { get; set; } = 2;
    }
}