namespace ScreenPeekr.Tray;

internal sealed class WebhookInstructionsWindow : Form
{
    public WebhookInstructionsWindow()
    {
        Text = "Setting Up a Discord Webhook for ScreenPeekr";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(600, 450);

        var instructions = new RichTextBox
        {
            Left = 12,
            Top = 12,
            Width = 576,
            Height = 380,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Font = new System.Drawing.Font("Segoe UI", 10F),
            BackColor = System.Drawing.Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        // RTF formatted text with rich formatting
        instructions.Rtf = @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}}
{\colortbl ;\red0\green0\blue255;\red255\green0\blue0;}
\viewkind4\uc1\pard\cf1\b\fs24 Setting Up a Discord Webhook for ScreenPeekr\par
\par
\cf0\b0\fs20 \b 1. Create a Private Discord Server\b0\par
\pard\li360\bullet - Open Discord.\par
- Click the \b +\b0 button on the left sidebar.\par
- Click \b Create My Own\b0.\par
- Click \b For me and my friends\b0.\par
- Enter a server name and click \b Create\b0.\par
\pard\par
\b 2. Create a Webhook\b0\par
\pard\li360\bullet - Open the channel where you want screenshots to be sent (such as \b #general\b0).\par
- Click the gear icon next to the channel.\par
- Click \b Integrations\b0.\par
- Click \b Webhooks\b0.\par
- Click \b Create Webhook\b0.\par
- Give the webhook a name (for example, \b ScreenPeekr\b0).\par
- Click \b Save Changes\b0.\par
\pard\par
\b 3. Copy the Webhook URL\b0\par
\pard\li360\bullet - Click on the webhook you just created.\par
- Click \b Copy Webhook URL\b0.\par
\pard\par
\b 4. Add the Webhook to ScreenPeekr\b0\par
\pard\li360\bullet - Open \b ScreenPeekr\b0.\par
- Go to \b Settings\b0.\par
- Find the \b Webhook URL\b0 setting.\par
- Paste the copied Discord webhook URL into the field.\par
- Save your settings.\par
\pard\par
\pard\li0\i ScreenPeekr will now send screenshots directly to your Discord channel whenever screenshot uploading is enabled.\i0  \cf2\b Keep your webhook URL private\b0\cf0, as anyone with the URL can send messages to the channel.\par
}";

        var closeButton = new Button
        {
            Text = "Close",
            Left = 500,
            Width = 88,
            Top = 400,
            DialogResult = DialogResult.OK
        };

        Controls.AddRange(new Control[] { instructions, closeButton });
    }
}
