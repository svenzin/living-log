using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public static class Categories
    {
        public static bool IsSync(Category c)
        {
            return (c == LivingLog_Startup) || (c == LivingLog_Sync);
        }

        public static readonly Category LivingLog_Startup = new Category() { Id = 0, Name = "LivingLog_Startup" };
        public static readonly Category LivingLog_Sync    = new Category() { Id = 10, Name = "LivingLog_Sync" };
        public static readonly Category LivingLog_Exit    = new Category() { Id = 11, Name = "LivingLog_Exit" };

        public static readonly Category Mouse_Move        = new Category() { Id = 1, Name = "Mouse_Move" };
        public static readonly Category Mouse_Down        = new Category() { Id = 2, Name = "Mouse_Down" };
        public static readonly Category Mouse_Up          = new Category() { Id = 3, Name = "Mouse_Up" };
        public static readonly Category Mouse_Click       = new Category() { Id = 4, Name = "Mouse_Click" };
        public static readonly Category Mouse_DoubleClick = new Category() { Id = 5, Name = "Mouse_DoubleClick" };
        public static readonly Category Mouse_Wheel       = new Category() { Id = 6, Name = "Mouse_Wheel" };

        public static readonly Category Keyboard_KeyDown  = new Category() { Id = 7, Name = "Keyboard_KeyDown" };
        public static readonly Category Keyboard_KeyUp    = new Category() { Id = 8, Name = "Keyboard_KeyUp" };
        public static readonly Category Keyboard_KeyPress = new Category() { Id = 9, Name = "Keyboard_KeyPress" };

        private static Dictionary<int, Category> categories;

        static Categories()
        {
            categories = new Dictionary<int, Category>()
            {
                { LivingLog_Startup.Id , LivingLog_Startup },
                { LivingLog_Sync.Id    , LivingLog_Sync    },
                { LivingLog_Exit.Id    , LivingLog_Exit    },
                { Mouse_Move.Id        , Mouse_Move        },
                { Mouse_Down.Id        , Mouse_Down        },
                { Mouse_Up.Id          , Mouse_Up          },
                { Mouse_Click.Id       , Mouse_Click       },
                { Mouse_DoubleClick.Id , Mouse_DoubleClick },
                { Mouse_Wheel.Id       , Mouse_Wheel       },
                { Keyboard_KeyDown.Id  , Keyboard_KeyDown  },
                { Keyboard_KeyUp.Id    , Keyboard_KeyUp    },
                { Keyboard_KeyPress.Id , Keyboard_KeyPress },
            };

            parser.ActivityParser.SetParser(LivingLog_Startup, parser.LivingParser.TryParse);
            parser.ActivityParser.SetParser(LivingLog_Sync, parser.LivingParser.TryParse);
            parser.ActivityParser.SetParser(LivingLog_Exit, parser.LivingParser.TryParse);

            parser.ActivityParser.SetParser(Mouse_Move, parser.MouseParser.MoveData.TryParse);
            parser.ActivityParser.SetParser(Mouse_Down, parser.MouseParser.ButtonData.TryParse);
            parser.ActivityParser.SetParser(Mouse_Up, parser.MouseParser.ButtonData.TryParse);
            parser.ActivityParser.SetParser(Mouse_Click, parser.MouseParser.ButtonData.TryParse);
            parser.ActivityParser.SetParser(Mouse_DoubleClick, parser.MouseParser.ButtonData.TryParse);
            parser.ActivityParser.SetParser(Mouse_Wheel, parser.MouseParser.WheelData.TryParse);

            parser.ActivityParser.SetParser(Keyboard_KeyDown, parser.KeyboardParser.KeyData.TryParse);
            parser.ActivityParser.SetParser(Keyboard_KeyUp, parser.KeyboardParser.KeyData.TryParse);
            parser.ActivityParser.SetParser(Keyboard_KeyPress, parser.KeyboardParser.PressData.TryParse);
        }

        public static Category get(int id)
        {
            Category result;
            if (!categories.TryGetValue(id, out result)) return null;
            return result;
        }
    }
}