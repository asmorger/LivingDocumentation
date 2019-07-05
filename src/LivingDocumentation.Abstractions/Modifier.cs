﻿using System;

namespace LivingDocumentation
{
    [Flags]
    public enum Modifier
    {
        Internal = 1 << 0,

        Public = 1 << 1,

        Private = 1 << 2,

        Protected = 1 << 3,

        Static = 1 << 4,

        Abstract = 1 << 5,

        Override = 1 << 6,

        Readonly = 1 << 7,

        Async = 1 << 8,
    }
}
