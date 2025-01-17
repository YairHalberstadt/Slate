﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using IdentityModel.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Font;
using MLEM.Misc;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using MonoScene.Graphics;
using MonoScene.Graphics.Pipeline;
using Slate.Client.Networking;
using Slate.Client.UI.ViewModels;
using Slate.Client.UI.Views;
using Keyboard = Microsoft.Xna.Framework.Input.Keyboard;

namespace Slate.Client
{
    public class RudeEngineGame : Microsoft.Xna.Framework.Game
    {
	    public static Task<GameTime> NextUpdate;
	    private TaskCompletionSource<GameTime> ThisUpdateSource = new();
        
        private readonly Options _options;
        
        private int _nativeScreenWidth;
        private int _nativeScreenHeight;

        private readonly PBREnvironment _lightsAndFog = PBREnvironment.CreateDefault();
        private DeviceModelCollection _testModel;
        private readonly ModelInstance[] _test = new ModelInstance[5 * 5];
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private UiSystem _uiSystem;

        public RudeEngineGame(Options options)
        {
            _options = options;
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1366;
            _graphics.PreferredBackBufferHeight = 768;
            _graphics.PreparingDeviceSettings += graphics_PreparingDeviceSettings;
            Window.ClientSizeChanged += Window_ClientSizeChanged;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            NextUpdate = ThisUpdateSource.Task;
        }

        private void Window_ClientSizeChanged(object? sender, EventArgs e)
        {

        }

        private void graphics_PreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs e)
        {
            _nativeScreenWidth = _graphics.PreferredBackBufferWidth;
            _nativeScreenHeight = _graphics.PreferredBackBufferHeight;

            _graphics.PreferMultiSampling = true;
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 8;
        }

        protected override void LoadContent()
        {
            this.IsMouseVisible = true;

            SpriteFont font = Content.Load<SpriteFont>("Segoe_UI_15_Bold");
            //Viewport viewport = GraphicsDevice.Viewport;
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            var uiText = Content.Load<Texture2D>("UI/RPG_GUI_v1");
            var background = Content.Load<Texture2D>("UI/Paper_Background");
            var uiStyle = new UntexturedStyle(_spriteBatch)
            {
                Font = new GenericSpriteFont(font),
                TextFieldTexture = new NinePatch(new TextureRegion(uiText, 812, 612, 168, 34), 12, NinePatchMode.Tile),
                PanelTexture = new NinePatch(background, 0, NinePatchMode.Tile)
            };

            uiStyle.Font = new GenericSpriteFont(font);
            _uiSystem = new UiSystem(this, uiStyle);
            
            var loginViewModel = new LoginViewModel(_options.AuthServer)
            {
                Username = "atomicblom",
                Password = "password"
            };
            loginViewModel.LoggedIn += LoginViewModelOnLoggedIn;
            Task.Run(loginViewModel.OnNavigatedTo);

            _uiSystem.Add(nameof(LoginView), LoginView.CreateView(loginViewModel));

            var gltfFactory = new GltfModelFactory(GraphicsDevice);
            _testModel = gltfFactory.LoadModel(Path.Combine($"Content", "Cell100.glb"));
        }

        private async void LoginViewModelOnLoggedIn(object? sender, TokenResponse e)
        {
	        var gameConnection = new GameConnection(_options.GameServer, _options.GameServerPort);
	        var connectionResult = await gameConnection.Connect(e.AccessToken);
	        if (connectionResult.WasSuccessful)
	        {
		        _uiSystem.Get(nameof(LoginView))
			        .FadeOut(duration: TimeSpan.FromSeconds(5), remove: true);

                _uiSystem.Remove(nameof(LoginView));
                var characterListViewModel = new CharacterListViewModel(gameConnection);
                _uiSystem.Add(nameof(CharacterListView), CharacterListView.CreateView(characterListViewModel));
                await Task.Run(characterListViewModel.OnNavigatedTo);
            }
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();

            _testModel?.Dispose();
            _testModel = null!;
        }

        protected override void Update(GameTime gameTime)
        {
	        var thisUpdate = ThisUpdateSource;
	        ThisUpdateSource = new();
	        NextUpdate = ThisUpdateSource.Task;
            thisUpdate.SetResult(gameTime);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            if (Keyboard.GetState().IsKeyDown(Keys.F3))
            {
                foreach (var reloadablePanel in _uiSystem.GetRootElements().Select(re => re.Element).OfType<ReloadablePanel>())
                {
                    reloadablePanel.Rebuild();
                }
            }
            
            _uiSystem.Update(gameTime);

            for (int z = 0; z < 5; ++z)
            {
                for (int x = 0; x < 5; ++x)
                {
                    var index = z * 5 + x;
                    _test[index] = _testModel.DefaultModel.CreateInstance();
                    _test[index].WorldMatrix = Matrix.CreateTranslation(x * 100, 0, z * 100);
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _uiSystem.DrawEarly(gameTime, _spriteBatch);

            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            var camPos = new Vector3(0, 25, 0);
            var modelPosition = new Vector3(50f, 0, 50f);

            var camX = Matrix.CreateWorld(Vector3.UnitY * 10, modelPosition - camPos, Vector3.UnitY);

            var dc = new ModelDrawingContext(this.GraphicsDevice)
            {
                NearPlane = 0.1f
            };
            
            dc.SetCamera(camX);
            
            dc.DrawSceneInstances(_lightsAndFog,
                _test);
            
            this._uiSystem.Draw(gameTime, _spriteBatch);

            base.Draw(gameTime);
        }
    }
}
