/**
 * Collaborative Whiteboard Application
 * This JavaScript file handles all the client-side functionality including:
 * - Canvas drawing operations
 * - SignalR real-time communication
 * - User interface interactions
 * - Session management
 */

class CollaborativeWhiteboard {
  constructor() {
    // Canvas and drawing context
    this.canvas = document.getElementById('whiteboard')
    this.ctx = this.canvas.getContext('2d')

    // Drawing state
    this.isDrawing = false
    this.currentTool = 'pen'
    this.currentColor = '#000000'
    this.currentLineWidth = 2
    this.lastX = 0
    this.lastY = 0

    // Session state
    this.currentSessionId = null
    this.currentUserId = 'User_' + Math.random().toString(36).substr(2, 9)

    // SignalR connection - This is the heart of real-time communication
    this.connection = null

    // Initialize the application
    this.init()
  }

  async init() {
    this.setupCanvas()
    this.setupUI()
    await this.setupSignalR()
    this.loadSessions()

    // Set current user display
    document.getElementById('currentUser').textContent = `User: ${this.currentUserId}`
  }

  setupCanvas() {
    // Set canvas size and styling
    const container = this.canvas.parentElement
    this.canvas.width = 1200
    this.canvas.height = 600

    // Configure drawing context
    this.ctx.lineCap = 'round'
    this.ctx.lineJoin = 'round'

    // Mouse events for desktop
    this.canvas.addEventListener('mousedown', (e) => this.startDrawing(e))
    this.canvas.addEventListener('mousemove', (e) => this.draw(e))
    this.canvas.addEventListener('mouseup', (e) => this.stopDrawing(e))
    this.canvas.addEventListener('mouseout', () => this.stopDrawing())

    // Touch events for mobile
    this.canvas.addEventListener('touchstart', (e) => {
      e.preventDefault()
      const touch = e.touches[0]
      const mouseEvent = new MouseEvent('mousedown', {
        clientX: touch.clientX,
        clientY: touch.clientY,
      })
      this.canvas.dispatchEvent(mouseEvent)
    })

    this.canvas.addEventListener('touchmove', (e) => {
      e.preventDefault()
      const touch = e.touches[0]
      const mouseEvent = new MouseEvent('mousemove', {
        clientX: touch.clientX,
        clientY: touch.clientY,
      })
      this.canvas.dispatchEvent(mouseEvent)
    })

    this.canvas.addEventListener('touchend', (e) => {
      e.preventDefault()
      const mouseEvent = new MouseEvent('mouseup', {})
      this.canvas.dispatchEvent(mouseEvent)
    })
  }

  setupUI() {
    // Tool selection
    document.getElementById('penTool').addEventListener('click', () => {
      this.currentTool = 'pen'
      this.updateToolButtons()
      this.canvas.style.cursor = 'crosshair'
    })

    document.getElementById('eraserTool').addEventListener('click', () => {
      this.currentTool = 'eraser'
      this.updateToolButtons()
      this.canvas.style.cursor = 'grab'
    })

    // Color picker
    document.getElementById('colorPicker').addEventListener('change', (e) => {
      this.currentColor = e.target.value
    })

    // Color presets
    document.querySelectorAll('.color-preset').forEach((preset) => {
      preset.addEventListener('click', () => {
        this.currentColor = preset.dataset.color
        document.getElementById('colorPicker').value = this.currentColor
      })
    })

    // Brush size
    const brushSize = document.getElementById('brushSize')
    const brushSizeValue = document.getElementById('brushSizeValue')

    brushSize.addEventListener('input', (e) => {
      this.currentLineWidth = parseInt(e.target.value)
      brushSizeValue.textContent = this.currentLineWidth
    })

    // Session controls
    document.getElementById('createSession').addEventListener('click', () => this.createSession())
    document
      .getElementById('loadSession')
      .addEventListener('click', () => this.loadSelectedSession())
    document.getElementById('clearBoard').addEventListener('click', () => this.clearBoard())
    document.getElementById('saveSession').addEventListener('click', () => this.saveSession())
  }

  updateToolButtons() {
    document.querySelectorAll('.tool-btn').forEach((btn) => btn.classList.remove('active'))
    document.getElementById(this.currentTool + 'Tool').classList.add('active')
  }

  /**
   * Setup SignalR Connection
   * This establishes the real-time communication channel with the server
   */
  async setupSignalR() {
    try {
      // Create SignalR connection to our WhiteboardHub
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/whiteboardhub') // This matches the URL in Program.cs
        .withAutomaticReconnect() // Automatically reconnect if connection is lost
        .build()

      // Set up event handlers for receiving messages from the server
      this.setupSignalREventHandlers()

      // Start the connection
      await this.connection.start()
      console.log('SignalR Connected')

      this.updateConnectionStatus('Connected', 'status-connected')
    } catch (err) {
      console.error('SignalR Connection Error: ', err)
      this.updateConnectionStatus('Connection Failed', 'status-disconnected')

      // Retry connection after 5 seconds
      setTimeout(() => this.setupSignalR(), 5000)
    }
  }

  /**
   * Setup SignalR Event Handlers
   * These handlers define what happens when we receive messages from other clients
   */
  setupSignalREventHandlers() {
    // Handle receiving drawing actions from other users
    this.connection.on('ReceiveDrawingAction', (drawingData) => {
      console.log('Received drawing action:', drawingData)
      this.drawRemoteAction(drawingData)
    })

    // Handle board clear from other users
    this.connection.on('ClearBoard', (data) => {
      console.log('Board cleared by:', data.UserId)
      this.clearCanvas()
    })

    // Handle user joining session
    this.connection.on('UserJoined', (data) => {
      console.log('User joined:', data.UserId)
      this.updateUsersList()
    })

    // Handle user leaving session
    this.connection.on('UserLeft', (data) => {
      console.log('User left:', data.UserId)
      this.updateUsersList()
    })

    // Handle connection state changes
    this.connection.onreconnecting((error) => {
      console.log('SignalR Reconnecting:', error)
      this.updateConnectionStatus('Reconnecting...', 'status-disconnected')
    })

    this.connection.onreconnected(() => {
      console.log('SignalR Reconnected')
      this.updateConnectionStatus('Connected', 'status-connected')

      // Rejoin current session if we have one
      if (this.currentSessionId) {
        this.joinSession(this.currentSessionId)
      }
    })

    this.connection.onclose((error) => {
      console.log('SignalR Disconnected:', error)
      this.updateConnectionStatus('Disconnected', 'status-disconnected')
    })
  }

  updateConnectionStatus(status, className) {
    const statusElement = document.getElementById('connectionStatus')
    statusElement.textContent = status
    statusElement.className = className
  }

  updateUsersList() {
    // In a real application, you'd track connected users
    // For now, just show that users are online
    const usersList = document.getElementById('usersList')
    usersList.innerHTML = `<div class="user-item">${this.currentUserId} (You)</div>`
  }

  /**
   * Drawing Functions
   */
  getMousePos(e) {
    const rect = this.canvas.getBoundingClientRect()
    return {
      x: e.clientX - rect.left,
      y: e.clientY - rect.top,
    }
  }

  startDrawing(e) {
    if (!this.currentSessionId) {
      alert('Please create or load a session first!')
      return
    }

    this.isDrawing = true
    const pos = this.getMousePos(e)
    this.lastX = pos.x
    this.lastY = pos.y
  }

  draw(e) {
    if (!this.isDrawing) return

    const pos = this.getMousePos(e)

    // Draw locally first for immediate feedback
    this.drawLine(
      this.lastX,
      this.lastY,
      pos.x,
      pos.y,
      this.currentColor,
      this.currentLineWidth,
      this.currentTool
    )

    // Send drawing action to other clients via SignalR
    this.sendDrawingAction(this.lastX, this.lastY, pos.x, pos.y)

    this.lastX = pos.x
    this.lastY = pos.y
  }

  stopDrawing(e) {
    if (!this.isDrawing) return
    this.isDrawing = false

    // Save final stroke to DB
    if (e) {
      const pos = this.getMousePos(e)
      this.saveDrawingAction({
        actionType: this.currentTool,
        startX: this.lastX,
        startY: this.lastY,
        endX: pos.x,
        endY: pos.y,
        color: this.currentColor,
        lineWidth: this.currentLineWidth,
      })
    }
  }

  drawLine(startX, startY, endX, endY, color, lineWidth, tool) {
    this.ctx.globalCompositeOperation = tool === 'eraser' ? 'destination-out' : 'source-over'
    this.ctx.strokeStyle = color
    this.ctx.lineWidth = tool === 'eraser' ? lineWidth * 2 : lineWidth

    this.ctx.beginPath()
    this.ctx.moveTo(startX, startY)
    this.ctx.lineTo(endX, endY)
    this.ctx.stroke()
  }

  /**
   * Draw action received from another user
   */
  drawRemoteAction(drawingData) {
    if (drawingData.ActionType === 'clear') {
      this.clearCanvas()
    } else {
      this.drawLine(
        drawingData.StartX,
        drawingData.StartY,
        drawingData.EndX,
        drawingData.EndY,
        drawingData.Color,
        drawingData.LineWidth,
        drawingData.ActionType
      )
    }
  }

  /**
   * Send drawing action to server via SignalR
   * This is where local drawing gets shared with other users
   */
  async sendDrawingAction(startX, startY, endX, endY) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('SendDrawingAction', {
          SessionId: this.currentSessionId,
          UserId: this.currentUserId,
          ActionType: this.currentTool,
          StartX: startX,
          StartY: startY,
          EndX: endX,
          EndY: endY,
          Color: this.currentColor,
          LineWidth: this.currentLineWidth,
        })
      } catch (err) {
        console.error('Error sending drawing action:', err)
      }
    }
  }

  /**
   * Session Management Functions
   */
  async createSession() {
    const sessionName = document.getElementById('sessionName').value.trim()
    if (!sessionName) {
      alert('Please enter a session name!')
      return
    }

    try {
      const response = await fetch('/api/Home/CreateSession', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name: sessionName }),
      })

      const result = await response.json()

      if (result.success) {
        this.currentSessionId = result.sessionId
        await this.joinSession(this.currentSessionId)
        this.clearCanvas()
        this.updateSessionStatus(`Session: ${sessionName}`)
        this.loadSessions() // Refresh session list

        // Clear session name input
        document.getElementById('sessionName').value = ''

        alert('Session created successfully!')
      } else {
        alert('Error creating session: ' + result.error)
      }
    } catch (error) {
      console.error('Error creating session:', error)
      alert('Error creating session. Please try again.')
    }
  }

  async loadSessions() {
    try {
      const response = await fetch('/api/Home/GetSessions')
      const result = await response.json()

      if (result.success) {
        const sessionList = document.getElementById('sessionList')
        sessionList.innerHTML = '<option value="">Select a session...</option>'

        result.sessions.forEach((session) => {
          const option = document.createElement('option')
          option.value = session.id
          option.textContent = `${session.name} (${new Date(
            session.lastModified
          ).toLocaleDateString()})`
          sessionList.appendChild(option)
        })
      }
    } catch (error) {
      console.error('Error loading sessions:', error)
    }
  }

  async loadSelectedSession() {
    const sessionId = document.getElementById('sessionList').value
    if (!sessionId) {
      alert('Please select a session to load!')
      return
    }

    try {
      const response = await fetch(`/api/Home/LoadSession/${sessionId}`)
      alert('Loading session. This may take a moment...')
      alert(response.status)
      const result = await response.json()

      if (result.success) {
        this.currentSessionId = sessionId
        await this.joinSession(sessionId)

        // Clear canvas and redraw from saved actions
        this.clearCanvas()
        this.redrawFromActions(result.drawingActions)

        this.updateSessionStatus(`Session: ${result.session.name}`)
        alert('Session loaded successfully!')
      } else {
        alert('Error loading session: ' + result.error)
      }
    } catch (error) {
      console.error('Error loading session:', error)
      alert('Error loading session. Please try again.')
    }
  }

  /**
   * Redraw canvas from saved drawing actions
   * This happens when loading a saved session
   */
  redrawFromActions(actions) {
    actions.forEach((action) => {
      if (action.ActionType === 'clear') {
        this.clearCanvas()
      } else {
        this.drawLine(
          action.startX,
          action.startY,
          action.endX,
          action.endY,
          action.color,
          action.lineWidth,
          action.actionType
        )
      }
    })
  }

  async joinSession(sessionId) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('JoinSession', sessionId, this.currentUserId)
        this.updateUsersList()
      } catch (err) {
        console.error('Error joining session:', err)
      }
    }
  }

  async clearBoard() {
    if (!this.currentSessionId) {
      alert('Please create or load a session first!')
      return
    }

    if (confirm('Are you sure you want to clear the board? This action cannot be undone.')) {
      this.clearCanvas()

      // Send clear action to other users
      if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
        try {
          await this.connection.invoke('ClearBoard', this.currentSessionId, this.currentUserId)
        } catch (err) {
          console.error('Error clearing board:', err)
        }
      }
    }
  }

  clearCanvas() {
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height)
  }

  /**
   * Save a drawing action to the database
   */
  async saveDrawingAction(action) {
    if (!this.currentSessionId) return

    try {
      await fetch('/api/Home/SaveDrawingAction', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          sessionId: this.currentSessionId,
          actionType: action.actionType,
          startX: action.startX,
          startY: action.startY,
          endX: action.endX,
          endY: action.endY,
          color: action.color,
          lineWidth: action.lineWidth,
          userId: this.currentUserId,
        }),
      })
    } catch (error) {
      console.error('Error saving drawing action:', error)
    }
  }

  async saveSession() {
    if (!this.currentSessionId) {
      alert('No active session to save!')
      return
    }

    // In this implementation, sessions are automatically saved to the database
    // when drawing actions occur. This could trigger a manual save or backup.
    alert('Session is automatically saved with each drawing action!')
  }

  updateSessionStatus(status) {
    document.getElementById('currentSession').textContent = status
  }
}

// Initialize the application when the page loads
document.addEventListener('DOMContentLoaded', () => {
  new CollaborativeWhiteboard()
})
