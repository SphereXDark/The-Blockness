﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TWP_Shared
{
    public static class InputManager
    {
        private static Game Game;
        
        private static int moveStep = 5;
        private static float moveSensitivity = 0.5f;

        private static List<GestureSample> gestures;
        private static KeyboardState prevKB;
        private static MouseState prevMouse;

        public static void Initialize(Game game)
        {
            Game = game;
            Update();
        }

        public static void Update()
        {
            gestures = GetGestures().ToList();
            prevMouse = Mouse.GetState();
            prevKB = Keyboard.GetState();
        }
        private static IEnumerable<GestureSample> GetGestures()
        {
            while (TouchPanel.IsGestureAvailable)
                yield return TouchPanel.ReadGesture();
        }

        public static Point? GetTapPosition()
        {
            // Touch screen first
            foreach (var gesture in gestures)
                if (gesture.GestureType == GestureType.Tap)
                    return gesture.Position.ToPoint();

            // Then mouse
            if (prevMouse.LeftButton == ButtonState.Released && Mouse.GetState().LeftButton == ButtonState.Pressed &&  Game.IsActive)
            {
                Point mousePoint = Mouse.GetState().Position;
                if (Game.GraphicsDevice.Viewport.Bounds.Contains(mousePoint))
                    return mousePoint;
            }

            return null;
        }

        public static Vector2 GetDragVector()
        {
            Vector2 result = Vector2.Zero;

            foreach (var gesture in gestures.Where(x => x.GestureType == GestureType.FreeDrag))
                result += gesture.Delta * moveSensitivity;

            if (Keyboard.GetState().IsKeyDown(Keys.Right))  result.X += moveStep;
            if (Keyboard.GetState().IsKeyDown(Keys.Left))   result.X -= moveStep;
            if (Keyboard.GetState().IsKeyDown(Keys.Down))   result.Y += moveStep;
            if (Keyboard.GetState().IsKeyDown(Keys.Up))     result.Y -= moveStep;

            return result;
        }
    }
}