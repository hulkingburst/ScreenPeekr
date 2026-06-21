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

        var instructions = new TextBox
        {
            Text = @"Setting Up a Discord Webhook for ScreenPeekr

1. Create a Private Discord Server
   - Open Discord.
   - Click the + button on the left sidebar.
   - Click Create My Own.
   - Click For me and my friends.
   - Enter a server name and click Create.

2. Create a Webhook
   - Open the channel where you want screenshots to be sent (such as #general).
   - Click the gear icon next to the channel.
   - Click Integrations.
   - Click Webhooks.
   - Click Create Webhook.
   - Give the webhook a name (for example, ScreenPeekr).
   - Click Save Changes.

3. Copy the Webhook URL
   - Click on the webhook you just created.
   - Click Copy Webhook URL.

4. Add the Webhook to ScreenPeekr
   - Open ScreenPeekr.
   - Go to Settings.
   - Find the Webhook URL setting.
   - Paste the copied Discord webhook URL into the field.
   - Save your settings.

ScreenPeekr will now send screenshots directly to your Discord channel whenever screenshot uploading is enabled. Keep your webhook URL private, as anyone with the URL can send messages to the channel.",
            Left = 12,
            Top = 12,
            Width = 576,
            Height = 380,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new System.Drawing.Font("Segoe UI", 10F)
        };

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
