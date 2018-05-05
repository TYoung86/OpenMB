﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMOFGameEngine.Screen
{
    public interface IScreen
    {
        event Action OnScreenExit;
        void Init();
        void Run();
        void Update(float timeSinceLastFrame);
        void Exit();
    }
}
