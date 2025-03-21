
using System;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

class PixelArtPictureBox : PictureBox
{
    protected override void OnPaint(PaintEventArgs pe)
    {
        if (Image != null)
        {
            pe.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            pe.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            int drawSize = 192;
            int x = (Width - drawSize) / 2; // Center horizontally
            int y = (Height - drawSize) / 2; // Center vertically

            pe.Graphics.DrawImage(Image, new Rectangle(x, y, drawSize, drawSize));
        }
        else
        {
            base.OnPaint(pe);
        }
    }
}

public class SpriteAnimation
{
    private Image spriteSheet; // Full spritesheet image
    private System.Windows.Forms.Timer animationTimer;
    private PictureBox pictureBox;
    private int frameWidth, frameHeight;
    private int totalFrames;
    private int currentFrame = 0;
    private Color backgroundColor = Color.DarkGray; // Set background color

    public SpriteAnimation(PictureBox pictureBox, string imagePath, int frameWidth = 16, int frameHeight = 16, int fps = 10)
    {

        this.pictureBox = pictureBox;
        this.spriteSheet = Image.FromFile(imagePath);
        frameWidth = spriteSheet.Height;
        frameHeight = spriteSheet.Height;

        this.frameWidth = frameWidth;
        this.frameHeight = frameHeight;

        // Calculate the total frames based on the image width
        this.totalFrames = spriteSheet.Width / frameWidth;

        // Make sure we don’t divide by zero or exceed bounds
        if (this.totalFrames <= 0)
            this.totalFrames = 1;

        // Setup timer for animation
        animationTimer = new System.Windows.Forms.Timer();
        animationTimer.Interval = 1000 / fps;
        animationTimer.Tick += UpdateFrame;
        animationTimer.Start();
    }

    private void UpdateFrame(object sender, EventArgs e)
    {
        if (spriteSheet != null && totalFrames > 0)
        {
            Bitmap frame = new Bitmap(frameWidth, frameHeight);
            using (Graphics g = Graphics.FromImage(frame))
            {
                // Fill background
                g.Clear(backgroundColor);

                // Draw the current frame from the spritesheet
                g.DrawImage(spriteSheet, new Rectangle(0, 0, frameWidth, frameHeight),
                    new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight),
                    GraphicsUnit.Pixel);
            }

            pictureBox.Image = frame; // Update the PictureBox
            currentFrame = (currentFrame + 1) % totalFrames; // Loop animation
        }
    }

    public void Stop()
    {
        animationTimer.Stop();
        pictureBox.Image = null;
    }
}
