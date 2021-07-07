﻿using System.Collections.Generic;

namespace Peon.Crafting
{
    public class Macro
    {
        public string         Name    { get; }
        public List<ActionId> Actions { get; } = new();

        public Macro(string name)
            => Name = name;

        public ActionInfo Step(int idx)
            => Actions[idx - 1].Use();

        public int Count
            => Actions.Count;
    }
}
