using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoFluid
{
    public class Particle
    {
        public Vector2 StartPosition;

        public Vector2 EndPosition;

        public Vector2 Velocity;

        public List<int> Neighbours { get; } = new List<int>();
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        
        private Texture2D _pointTexture;

        private List<Particle> _particles = new List<Particle>();

        private readonly Random _random = new Random();

        private int _width;
        private int _height;
        private const float _seabedGradient = 0.05f;
        private readonly Vector2 _gravity = new Vector2(0, 1);
        private const float _dt = 0.1f;
        private const float _targetSeparation = 20;
        private const float _targetSeparationSquared= _targetSeparation * _targetSeparation;
        private const float _radiusOfInterestSqrt = (_targetSeparation * 2);
        private const float _radiusOfInterest = _radiusOfInterestSqrt * _radiusOfInterestSqrt;
        private const int _iterationCount = 2;
        private const float _stiffness = 0.5f;
        const float _heatConst = 0.2f;
        private const float _damping = 1.0f;//0.995f;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _width = _graphics.GraphicsDevice.DisplayMode.Width;
            _height = _graphics.GraphicsDevice.DisplayMode.Height - 100;

            _graphics.PreferredBackBufferHeight = _height;
            _graphics.PreferredBackBufferWidth = _width;
            _graphics.ApplyChanges();

            for(int y = 0; y < 32; y++)
            {
                var rowOffset = (float)_random.NextDouble();
                for (int x = 3; x < 20; x++)
                {
                    _particles.Add(new Particle
                    {
                        StartPosition = new Vector2(
                            x * (_targetSeparation * 1.5f) + rowOffset,
                            y * (_targetSeparation * 1.5f))
                    });
                }
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pointTexture = Texture2D.FromFile(GraphicsDevice, "Content/blank.png");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Gravity and veloctiy update
            foreach(var particle in _particles)
            {
                particle.Velocity += _gravity * _dt;
                particle.Velocity *= _damping;
                particle.EndPosition = particle.StartPosition + particle.Velocity * _dt;
            }

            // Find neighbours
            Parallel.For(0, _particles.Count, i =>
            {
                var outer = _particles[i];
                outer.Neighbours.Clear();

                for (int j = 0; j < _particles.Count; j++)
                {
                    if (i == j) continue;

                    var inner = _particles[j];
                    var delta = outer.EndPosition - inner.EndPosition;

                    if (delta.LengthSquared() < _radiusOfInterest)
                    {
                        outer.Neighbours.Add(j);
                    }
                }
            });

            // Solve
            for (int iteration = 0; iteration < _iterationCount; iteration++)
            {
                foreach (var particle in _particles)
                {
                    foreach (var index in particle.Neighbours)
                    {
                        var neighbourPos = _particles[index].EndPosition;
                        var delta = particle.EndPosition - neighbourPos;

                        if (delta.LengthSquared() < _targetSeparationSquared)
                        {
                            var error = _targetSeparation - delta.Length();
                            delta.Normalize();
                            particle.EndPosition += delta * error * _stiffness;
                        }
                    }
                }
            }

            // Heat
            foreach (var particle in _particles)
            {
                lock (_random)
                {
                    particle.EndPosition.X += ((float)_random.NextDouble() - 0.5f) * _heatConst;
                    particle.EndPosition.Y += ((float)_random.NextDouble() - 0.5f) * _heatConst;
                }
            }

            // Boundaries
            foreach (var particle in _particles)
            {
                if (particle.EndPosition.X < 0) particle.EndPosition.X = 0;
                if (particle.EndPosition.X > _width) particle.EndPosition.X = _width;
                if (particle.EndPosition.Y < 0) particle.EndPosition.Y = 0;
                var slope = Slope(particle.EndPosition.X);
                if (particle.EndPosition.Y > _height - slope) particle.EndPosition.Y = _height - slope;
            }

            // Implicity velocity calc
            foreach(var particle in _particles)
            {
                particle.Velocity = (particle.EndPosition - particle.StartPosition) / _dt;
                particle.StartPosition = particle.EndPosition;
            }

            base.Update(gameTime);
        }

        private float Slope(float x)
        {
            return MathF.Max((x - 800) * _seabedGradient, 0.0f);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            foreach (var particle in _particles)
            {
                _spriteBatch.Draw(
                    _pointTexture,
                    new Rectangle((int)particle.EndPosition.X - 2, (int)particle.EndPosition.Y - 2, 5, 5),
                    Color.Blue);
            }
            
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
