﻿using System;
using System.Diagnostics;
using xnaMugen.IO;
using System.Collections.Generic;
using xnaMugen.Drawing;
using Microsoft.Xna.Framework;

namespace xnaMugen.Menus
{
	internal class MenuSystem : MainSystem
	{
		public MenuSystem(SubSystems subsystems)
			: base(subsystems)
		{
			var textfile = GetSubSystem<FileSystem>().OpenTextFile(@"data/system.def");
			var info = textfile.GetSection("info");
			var files = textfile.GetSection("files");

			MotifName = info.GetAttribute("name", string.Empty);
			MotifAuthor = info.GetAttribute("author", string.Empty);

			var fontmap = new Dictionary<int, Font>();

			var spritesystem = GetSubSystem<SpriteSystem>();

			var fontpath1 = files.GetAttribute<string>("font1", null);
			if (fontpath1 != null) fontmap[1] = spritesystem.LoadFont(fontpath1);

			var fontpath2 = files.GetAttribute<string>("font2", null);
			if (fontpath2 != null) fontmap[2] = spritesystem.LoadFont(fontpath2);

			var fontpath3 = files.GetAttribute<string>("font3", null);
			if (fontpath3 != null) fontmap[3] = spritesystem.LoadFont(fontpath3);

			FontMap = new FontMap(fontmap);

			var soundpath = @"data/" + files.GetAttribute<string>("snd");
			var spritepath = @"data/" + files.GetAttribute<string>("spr");
			var animpath = textfile.Filepath;

			TitleScreen = new TitleScreen(this, textfile.GetSection("Title Info"), spritepath, animpath, soundpath);
			TitleScreen.LoadBackgrounds("Title", textfile);

			VersusScreen = new VersusScreen(this, textfile.GetSection("VS Screen"), spritepath, animpath, soundpath);
			VersusScreen.LoadBackgrounds("Versus", textfile);

			SelectScreen = new SelectScreen(this, textfile.GetSection("Select Info"), spritepath, animpath, soundpath);
			SelectScreen.LoadBackgrounds("Select", textfile);

			CombatScreen = new CombatScreen(this);
			ReplayScreen = new RecordedCombatScreen(this);
            StoryboardScreen = new StoryboardScreen(this);

			CurrentScreen = null;
			m_newscreen = null;
			m_fade = 0;
			m_fadespeed = 0;
			m_eventqueue = new Queue<Events.Base>();
		}

		public void PostEvent(Events.Base theevent)
		{
			if (theevent == null) throw new ArgumentNullException(nameof(theevent));

			m_eventqueue.Enqueue(theevent);
		}

		private void SetScreen(Screen screen)
		{
			if (screen == null) throw new ArgumentNullException(nameof(screen));

			if (CurrentScreen != null)
			{
				m_newscreen = screen;

				PostEvent(new Events.FadeScreen(FadeDirection.Out));
			}
			else
			{
				CurrentScreen = screen;

				PostEvent(new Events.FadeScreen(FadeDirection.In));
			}
		}

		private void FadeInScreen(Screen screen)
		{
			if (screen == null) throw new ArgumentNullException(nameof(screen));

			screen.Reset();
			screen.FadingIn();

			m_fade = 0;
			m_fadespeed = screen.FadeInTime;
		}

		private void FadedInScreen(Screen screen)
		{
			if (screen == null) throw new ArgumentNullException(nameof(screen));

			screen.FadeInComplete();
		}

		private void FadeOutScreen(Screen screen)
		{
			if (screen == null) throw new ArgumentNullException(nameof(screen));

			screen.FadingOut();

			GetSubSystem<Input.InputSystem>().LoadInputState();

			m_fade = 1;
			m_fadespeed = -screen.FadeOutTime;
		}

		private void FadedOutScreen(Screen screen)
		{
			if (screen == null) throw new ArgumentNullException(nameof(screen));

			screen.FadeOutComplete();
		}

		public void Update(GameTime gametime)
		{
			RunEvents();

			DoFading();
			CurrentScreen.Update(gametime);
		}

		public void Draw(bool debugdraw)
		{
			CurrentScreen.Draw(debugdraw);
		}

		private void RunEvents()
		{
			while (m_eventqueue.Count > 0)
			{
				var e = m_eventqueue.Dequeue();

				if (e is Events.LoadReplay)
				{
					var ee = e as Events.LoadReplay;

					ReplayScreen.SetReplay(ee.Recording);
					GetMainSystem<Combat.FightEngine>().Set(ee.Recording.InitializationSettings);
				}

				if (e is Events.SwitchScreen)
				{
					var ee = e as Events.SwitchScreen;

					switch (ee.Screen)
					{
						case ScreenType.Select:
							SetScreen(SelectScreen);
							break;

						case ScreenType.Title:
							SetScreen(TitleScreen);
							break;

                        case ScreenType.Storyboard:
                            SetScreen(StoryboardScreen);
                            break;

						case ScreenType.Versus:
							SetScreen(VersusScreen);
							break;

						case ScreenType.Combat:
							SetScreen(CombatScreen);
							break;

						case ScreenType.Replay:
							SetScreen(ReplayScreen);
							break;
					}
				}

				if (e is Events.FadeScreen)
				{
					var ee = e as Events.FadeScreen;

					switch (ee.Direction)
					{
						case FadeDirection.In:
							FadeInScreen(CurrentScreen);
							break;

						case FadeDirection.Out:
							FadeOutScreen(CurrentScreen);
							break;
					}
				}

                if (e is Events.SetupCombatMode)
                {
                    var ee = e as Events.SetupCombatMode;

                    SelectScreen.CombatMode = ee.Mode;
                }

                if (e is Events.SetupStoryboard)
                {
                    var ee = (Events.SetupStoryboard)e;
                    StoryboardScreen.Storyboard = ee.Storyboard;
                    StoryboardScreen.Event = ee.Event;
                }

				if (e is Events.SetupCombat)
				{
					var ee = e as Events.SetupCombat;

					GetMainSystem<Combat.FightEngine>().Set(ee.Initialization);
                    VersusScreen.P1.Profile = ee.Initialization.Team1P1.Profile;
                    VersusScreen.P2.Profile = ee.Initialization.Team2P1.Profile;
				}
			}
		}

		private void DoFading()
		{
			if (m_fadespeed == 0)
			{
				GetSubSystem<Video.VideoSystem>().Tint = Color.White;
				return;
			}

			m_fade += 1.0f / m_fadespeed;

			if (m_fadespeed > 0)
			{
				m_fade = Math.Min(1, m_fade);

				if (m_fade == 1)
				{
					m_fadespeed = 0;

					FadedInScreen(CurrentScreen);
				}
			}

			if (m_fadespeed < 0)
			{
				m_fade = Math.Max(0, m_fade);

				if (m_fade == 0)
				{
					m_fadespeed = 0;

					FadedOutScreen(CurrentScreen);

					if (m_newscreen != null)
					{
						CurrentScreen = m_newscreen;
						m_newscreen = null;

						FadeInScreen(CurrentScreen);
					}
				}
			}

			GetSubSystem<Video.VideoSystem>().Tint = new Color(new Vector4(m_fade, m_fade, m_fade, m_fade));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (TitleScreen != null) TitleScreen.Dispose();

				if (VersusScreen != null) VersusScreen.Dispose();

				SelectScreen?.Dispose();

				if (FontMap != null) FontMap.Dispose();
			}

			base.Dispose(disposing);
		}

		public string MotifName { get; }

		public string MotifAuthor { get; }

		public FontMap FontMap { get; }

		public TitleScreen TitleScreen { get; }

		public SelectScreen SelectScreen { get; }

		public VersusScreen VersusScreen { get; }

		public CombatScreen CombatScreen { get; }

		public RecordedCombatScreen ReplayScreen { get; }

		public StoryboardScreen StoryboardScreen { get; }

		public Screen CurrentScreen { get; private set; }

		#region Fields

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Screen m_newscreen;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private float m_fade;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private float m_fadespeed;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly Queue<Events.Base> m_eventqueue;

		#endregion
	}
}