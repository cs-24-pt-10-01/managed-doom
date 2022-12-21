﻿using System;
using System.Runtime.ExceptionServices;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using DrippyAL;

namespace ManagedDoom.Silk
{
    public class SilkDoom : IDisposable
    {
        private CommandLineArgs args;

        private Config config;
        private GameContent content;

        private IWindow window;

        private GL gl;
        private SilkVideo video;

        private AudioDevice audioDevice;
        private SilkSound sound;
        private SilkMusic music;

        private SilkUserInput userInput;

        private Doom doom;

        public SilkDoom(CommandLineArgs args)
        {
            try
            {
                this.args = args;

                GlfwWindowing.RegisterPlatform();
                GlfwInput.RegisterPlatform();

                config = SilkConfigUtilities.GetConfig();
                content = new GameContent(args);

                config.video_screenwidth = Math.Clamp(config.video_screenwidth, 320, 3200);
                config.video_screenheight = Math.Clamp(config.video_screenheight, 200, 2000);

                var windowOptions = WindowOptions.Default;
                windowOptions.Size = new Vector2D<int>(config.video_screenwidth, config.video_screenheight);
                windowOptions.Title = ApplicationInfo.Title;
                windowOptions.WindowState = config.video_fullscreen ? WindowState.Fullscreen : WindowState.Normal;

                window = Window.Create(windowOptions);

                if (args.timedemo.Present)
                {
                    window.UpdatesPerSecond = 0;
                    window.FramesPerSecond = 0;
                    window.VSync = false;
                }
                else
                {
                    window.UpdatesPerSecond = 35;
                    window.FramesPerSecond = 35;
                    window.VSync = true;
                }

                window.Load += OnLoad;
                window.Update += OnUpdate;
                window.Render += OnRender;
                window.Resize += OnResize;
                window.Closing += OnClose;
            }
            catch (Exception e)
            {
                Dispose();
                ExceptionDispatchInfo.Throw(e);
            }
        }

        public void Run()
        {
            window.Run();
        }

        private void OnLoad()
        {
            gl = window.CreateOpenGL();
            gl.ClearColor(0.15F, 0.15F, 0.15F, 1F);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            window.SwapBuffers();

            video = new SilkVideo(config, content, window, gl);

            if (!args.nosound.Present && !(args.nosfx.Present && args.nomusic.Present))
            {
                audioDevice = new AudioDevice();
                if (!args.nosfx.Present)
                {
                    sound = new SilkSound(config, content, audioDevice);
                }
                if (!args.nomusic.Present)
                {
                    music = SilkConfigUtilities.GetMusicInstance(config, content, audioDevice);
                }
            }

            userInput = new SilkUserInput(config, window, this, !args.nomouse.Present);

            doom = new Doom(args, config, content, video, sound, music, userInput);
        }

        private void OnUpdate(double obj)
        {
            if (doom.Update() == UpdateResult.Completed)
            {
                window.Close();
            }
        }

        private void OnRender(double obj)
        {
            video.Render(doom);
        }

        private void OnResize(Vector2D<int> obj)
        {
            video.Resize(obj.X, obj.Y);
        }

        private void OnClose()
        {
            if (userInput != null)
            {
                userInput.Dispose();
                userInput = null;
            }

            if (music != null)
            {
                music.Dispose();
                music = null;
            }

            if (sound != null)
            {
                sound.Dispose();
                sound = null;
            }

            if (audioDevice != null)
            {
                audioDevice.Dispose();
                audioDevice = null;
            }

            if (video != null)
            {
                video.Dispose();
                video = null;
            }

            if (gl != null)
            {
                gl.Dispose();
                gl = null;
            }

            config.Save(ConfigUtilities.GetConfigPath());
        }

        public void KeyDown(Key key)
        {
            doom.PostEvent(new DoomEvent(EventType.KeyDown, SilkUserInput.SilkToDoom(key)));
        }

        public void KeyUp(Key key)
        {
            doom.PostEvent(new DoomEvent(EventType.KeyUp, SilkUserInput.SilkToDoom(key)));
        }

        public void Dispose()
        {
            if (window != null)
            {
                window.Close();
                window.Dispose();
                window = null;
            }
        }

        public string QuitMessage => doom.QuitMessage;
    }
}
