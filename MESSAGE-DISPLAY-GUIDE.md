# Message Display System Guide

## Overview

The message display system allows you to send custom messages to client PCs that appear as fullscreen overlays. The messages are displayed with:

- **Fullscreen transparent black overlay** (90% opacity)
- **Centered yellow text** (36pt Segoe UI Bold)
- **Blocked keyboard/mouse input** for the duration
- **Auto-close** after specified time (3-5 seconds)

## How to Send Messages

### 1. Send Custom Message (📨)

1. In the monitoring dashboard, double-click the **📨 icon** in the Actions column
2. Enter your custom message in the dialog
3. Click **Send Message**
4. The message will be queued and sent within 2 seconds

**Example messages:**

- "Please save your work immediately"
- "System maintenance in 5 minutes"
- "Break time - 10 minutes"
- "Class session ending soon"

### 2. Send Warning/Freeze (⚠)

1. Double-click the **⚠ icon** in the Actions column
2. Confirm the freeze action
3. The client screen will freeze for 3 seconds with a warning message

**Default warning message:**

```
⚠ ATTENTION: This screen has been frozen by the administrator for 3 seconds.
```

## Technical Details

### Message Display Features

- **Fullscreen overlay** - covers entire screen including taskbar
- **Input blocking** - uses Windows BlockInput API to temporarily disable keyboard/mouse
- **Auto-recovery** - automatically unblocks input after timer expires
- **Centered text** - message is displayed in the center of the screen
- **High visibility** - large yellow text on dark background
- **Non-intrusive** - runs on separate thread, doesn't block monitoring loop

### Duration Settings

- **Custom messages**: 5 seconds (configurable in MonitoringForm.cs)
- **Freeze warnings**: 3 seconds (configurable in MonitoringForm.cs)

### Safety Features

- Input is **always unblocked** when form closes
- Timer ensures **automatic closure** even if errors occur
- **Background thread** prevents blocking the monitoring system
- **Error handling** prevents crashes if display fails

## Customization

### Change Message Duration

Edit `MonitoringForm.cs`:

```csharp
// For custom messages (line ~345)
Duration = 5 // Change to desired seconds

// For freeze warnings (line ~367)
Duration = 3 // Change to desired seconds
```

### Change Text Appearance

Edit `MessageDisplayForm.cs`:

```csharp
lblMessage = new Label
{
    Font = new Font("Segoe UI", 36, FontStyle.Bold), // Change size/font
    ForeColor = Color.Yellow, // Change color
    // ...
};
```

### Change Overlay Opacity

Edit `MessageDisplayForm.cs`:

```csharp
this.Opacity = 0.90; // Change from 0.90 (90%) to desired value (0.0 - 1.0)
```

## Troubleshooting

### Message not appearing?

1. Check if client is connected (green highlight in list)
2. Wait 2 seconds after sending (client updates every 2 seconds)
3. Check server log for "Command queued for PC-X" message

### Message appears but input not blocked?

- Administrator privileges may be required on client PC
- BlockInput API requires sufficient permissions

### Message stays on screen too long?

- Timer may have failed - this is rare
- User can restart the client to clear any stuck messages

### Client stops responding after message?

- This should not happen - message runs on separate thread
- If it does, check client event log for errors

## Architecture

### Flow Diagram

```
Server                          Client
  │                              │
  ├─ User clicks 📨             │
  ├─ Enter message              │
  ├─ Queue command              │
  │                              │
  │  ◄─── Activity Update ───── │
  │                              │
  ├─ Send Command ─────────────►│
  │  (instead of ACK)            │
  │                              ├─ Parse command
  │                              ├─ Start thread
  │                              ├─ Show fullscreen form
  │                              ├─ Block input
  │                              ├─ Start timer
  │                              │  ... wait 5 seconds ...
  │                              ├─ Unblock input
  │                              └─ Close form
  │                              │
  │  ◄─── Next Update ────────── │
```

### Command Structure

```json
{
	"CommandType": "message", // or "freeze"
	"MessageText": "Your custom message here",
	"Duration": 5 // seconds
}
```

## Best Practices

1. **Keep messages short** - users can't interact during display
2. **Use clear language** - messages should be immediately understandable
3. **Test first** - try sending to one PC before broadcasting
4. **Avoid overuse** - only use for important notifications
5. **Monitor duration** - 3-5 seconds is usually enough
6. **Consider timing** - don't send during critical tasks

## Future Enhancements

Possible additions:

- [ ] Custom colors per message type
- [ ] Sound alerts
- [ ] Message acknowledgment tracking
- [ ] Broadcast to all PCs
- [ ] Scheduled messages
- [ ] Multi-line message support with word wrap
- [ ] Variable duration in UI
- [ ] Message history log
