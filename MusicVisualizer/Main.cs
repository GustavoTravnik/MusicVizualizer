using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NovaDll;
using NovaDll.AudioVisualizations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;

namespace Test2
{
    public class Main : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Nova_Audio player = new Nova_Audio();

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        private static String MUSIC_PATH = Path.Combine(Environment.CurrentDirectory, "Musics");

        int imageWidth = 500;
        float sumPower = 0;

        Nova_Particle[] particles = new Nova_Particle[QTD_BARS];
        List<Nova_Particle> emiters = new List<Nova_Particle>();
        List<Nova_Particle> emitersText = new List<Nova_Particle>();
        RenderTarget2D target;

        Boolean isShow = true;

        Dictionary<String, String> songs = new Dictionary<String, String>();
        List<Texture2D> arts = new List<Texture2D>();
        int index = 0;

        private float opacity = 0.05f;

        Texture2D starTexture;

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Nova_Functions.SetGraphics(graphics);
            Nova_Functions.SetGame(this);
            int width = 0;
            foreach(var x in GraphicsAdapter.Adapters)
            {
                width += x.CurrentDisplayMode.Width;
            }
           // Nova_Functions.ConfigureGraphics(true, true, false, SurfaceFormat.Color, DepthFormat.None);
            Nova_Functions.ChangeResolution(width, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height, false);
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            GetForm().Location = new System.Drawing.Point(0, 0);
            GetForm().Size = new Size(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            GetForm().TransparencyKey = System.Drawing.Color.Black;
            GetForm().BackColor = System.Drawing.Color.Black;
            GetForm().Opacity = 0f;
            GetForm().TopMost = true;
            GetForm().FormBorderStyle = FormBorderStyle.None;
        }

        public Form GetForm()
        {
            return Nova_Functions.GetWindowsFormFrom(Window.Handle);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Nova_Importer.SetContent(Content);
            Nova_Functions.SetViewport(GraphicsDevice);
            target = new RenderTarget2D(GraphicsDevice, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            Nova_Importer.LoadResource("font", "font");
            Nova_Importer.LoadResource("default", "default");
            Nova_Importer.LoadResource("bar", "bar");
            starTexture = Content.Load<Texture2D>("star");
            LoadParticles();
            player.StartCaptureAudioFromDefaultPlayerDevice();
            ThreadPool.QueueUserWorkItem(async (object o) =>
            {
                while (true)
                {
                    oldArray = player.GetCurrentLinearFrequencies();
                    await Task.Delay(32);
                }
            });
        }

        const int QTD_BARS = 1000;

        private void LoadParticles()
        {
            for (int i = 0; i < QTD_BARS; i++)
            {
                Nova_Particle p = new Nova_Particle();
                p.SetTexture(Nova_DataBase.GetTexture("bar"), SpriteEffects.None, Microsoft.Xna.Framework.Color.White);
                p.ParticleColor = Nova_Functions.HSVToColor((6f / (float)QTD_BARS * i), 1, 1);
               // p.Position = new Vector2(((float)Nova_Functions.View.Width / QTD_BARS) * i, Nova_Functions.View.Height / 2 - p.GetCurrentTexture().Height / 2);
                p.Position = new Vector2(((float)Nova_Functions.View.Width / QTD_BARS) * i, 0);
               // p.SetAngle(-i, Nova_Functions.GetCenterOf(p.GetCurrentTexture()));
               // p.SetDirectionRotation(400, 200, 0, 0, i, i, 0, 0, true);
                p.IsInflateBothSides = true;
                particles[i] = p;
            }
        }

        protected override void UnloadContent()
        {
            player.ClearInstance();
        }

        public void Controls()
        {
            if (GetAsyncKeyState(System.Windows.Forms.Keys.PageUp) == -0x8000)
            {
                opacity += 0.01f;
                if (opacity > 1) opacity = 1;
            }
            if (GetAsyncKeyState(System.Windows.Forms.Keys.PageDown) == -0x8000)
            {
                opacity -= 0.01f;
                if (opacity < 0) opacity = 0;
            }
        }
        bool[] states = new bool[200];
        public void CreateTextParticle()
        {
            InputSimulator simulator = new InputSimulator();
            int index = 0;
            foreach (WindowsInput.Native.VirtualKeyCode key in Enum.GetValues(typeof(WindowsInput.Native.VirtualKeyCode)))
            {
                if (simulator.InputDeviceState.IsHardwareKeyDown(key) && !states[index])
                {
                    Nova_Particle p = new Nova_Particle();
                    string keyName = Enum.GetName(typeof(WindowsInput.Native.VirtualKeyCode), key);
                    p.SetTextureFont(Nova_DataBase.GetFont("font"), keyName.Contains("_") ? keyName.Split('_')[1] : keyName, SpriteEffects.None, Microsoft.Xna.Framework.Color.White);
                    p.Position = new Vector2(-Nova_DataBase.GetFont("font").MeasureString(keyName).X / 2, GraphicsDevice.Viewport.Height / 4);
                  //  p.SetDirectionRotation(Nova_Functions.GetRandomNumber(-5, 5), Nova_Functions.GetRandomNumber(-5, 5), 0, 0, 0, 0, Nova_Functions.GetRandomNumber(1, 5), Nova_Functions.GetRandomNumber(-1, 5), false);
                    p.LifeTime = 7600;
                    p.SetFadeOut(7600);
                    p.SetInflateSpeed(1, 20, 1, 20, Nova_Particle.GrowingTypeEnum.asc);
                    p.IsAllColorsUntilDie = true;
                    p.InitialLifeTime = 7600;
                    p.SetDirectionSpeed(new Vector2(6, 0));
                    emitersText.Add(p);                    
                }
                states[index] = simulator.InputDeviceState.IsHardwareKeyDown(key);
                index++;
            } 
        }

        int timeShowed = 0;

        public void PlayPause()
        {
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Pause();
            }
            else if (MediaPlayer.State == MediaState.Paused || MediaPlayer.State == MediaState.Stopped)
            {
                MediaPlayer.Resume();
            }
        }

        private void PerformShowOpacity()
        {
            GetForm().Opacity = 0;
            isShow = true;
            timeShowed = 0;
            Nova_Functions.ChangeResolution(Nova_Functions.View.Width, 300, true);
            for (int i = 0; i < 256; i++)
            {
                particles[i].Position = new Vector2(imageWidth + 50 + (i * 5), 160);
            }
        }

        private void PerformNormalState()
        {
            GetForm().Opacity = 0.3f;
            timeShowed = 0;
            Nova_Functions.ChangeResolution(Nova_Functions.View.Width, 150, true);
            for (int i = 0; i < 256; i++)
            {
                particles[i].Position = new Vector2((i * 7.4f), 160);
            }
        }

        public void Next()
        {
            PerformShowOpacity();
            if (index < songs.Count - 1)
            {
                index++;
            }
            player.PlayMusic(songs.ToList()[index].Key);
        }

        public void Prev()
        {
            PerformShowOpacity();
            if (index > 0)
            {
                index--;
            }
            player.PlayMusic(songs.ToList()[index].Key);
        }

        float colorIndex = 0f;

        SpectrumBase.SpectrumPointData[] oldArray;

        public void UpdatePlayer()
        {
            float aux = sumPower;
            sumPower = 0;
            if (oldArray == null) return;
            for (int i = 0; i < QTD_BARS; i++)
            {
                float maxValue = (float)(oldArray[(int)Math.Ceiling(255f / (float)QTD_BARS * (float)i)].Value * 20);
                particles[i].CustomSizeHeight = (int)Nova_Functions.ApproxNumbers(particles[i].CustomSizeHeight, maxValue, 1 + maxValue / 20);
                if (i < 20)
                    sumPower += (float)oldArray[i].Value / 30;
            }
            colorIndex += 0.01f;
            if (colorIndex > 6)
            {
                colorIndex = 0f;
            }            
        }        

        protected override void Update(GameTime gameTime)
        {
            try
            {
                base.Update(gameTime);
                Controls();
                foreach (Nova_Particle bar in particles)
                {
                    bar.Update(gameTime, Matrix.CreateTranslation(0, 0, 0));
                }
                //Nova_Particle p = new Nova_Particle();
                //p.SetTexture(starTexture, SpriteEffects.None, Microsoft.Xna.Framework.Color.White);
                //p.Position = new Vector2(-p.GetCurrentTexture().Width, Nova_Functions.View.Height  - p.GetCurrentTexture().Height/2);
                //p.DestroyOnLeaveScreen = true;
                //int time = 3600;
                //p.LifeTime = time;
                //p.IsInflateBothSides = false;
                //p.InitialLifeTime = time;
                //p.SetFadeOut(time);
                ////p.SetInternalRotation(0, 2, Nova_Functions.GetCenterOf(p.GetCurrentTexture()), Nova_Particle.RotationDirectionEnum.clockwise);
                //p.SetInflateSize(0, -24 + (int)Math.Floor(sumPower)*5);
                //p.SpeedX = 20;
                //p.AcelerationFactorX = 0.9875f;
                //p.IsAllColorsUntilDie = true;
                ////p.SetDirectionRotation(4.0f, 2.0f, 0, 0, 0, 0, 1, 1, false);
                //emiters.Add(p);
                Nova_Particle.DoUpdateParticles(emiters, gameTime, Matrix.CreateTranslation(0, 0, 0));
               // CreateTextParticle();
                Nova_Particle.DoUpdateParticles(emitersText, gameTime, Matrix.CreateTranslation(0, 0, 0));
                UpdatePlayer();
            }
            catch
            {
                player.ClearInstance();
                player.StartCaptureAudioFromDefaultPlayerDevice();
            }
        }

        bool change = true;

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);
            GetForm().TopMost = true;
            GetForm().Opacity = opacity;
            if (change)
            {
                GetForm().Size = new Size(Nova_Functions.View.Width, Nova_Functions.View.Height);
                    
                GetForm().Show();
                GetForm().BringToFront();
            }
            change = false;
            GraphicsDevice.SetRenderTarget(target);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

            Nova_Particle.DoDrawParticles(emitersText, spriteBatch);
            Nova_Particle.DoDrawParticles(particles.ToList(), spriteBatch);
            //for ( int i = 0; i < emiters.Count; i++)
            //{
            //    Nova_Particle p = emiters[i];
            //    p.Draw(spriteBatch);
            //    if (p.LifeTime <= 0)
            //    {
            //        emiters.Remove(p);
            //        i--;
            //    }
            //}            
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            spriteBatch.Begin(0, BlendState.Additive);
            spriteBatch.Draw(target, Vector2.Zero, Microsoft.Xna.Framework.Color.White);
            spriteBatch.Draw(target, Nova_Functions.ReturnScreenRectangle(), null, Microsoft.Xna.Framework.Color.White, 0, Vector2.Zero, SpriteEffects.FlipVertically | SpriteEffects.FlipHorizontally, 1f);
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
