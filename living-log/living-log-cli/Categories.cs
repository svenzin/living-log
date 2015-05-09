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

        public static readonly Category LivingLog_Startup = new Category() { Id =  0, Name = "LivingLog_Startup" };
        public static readonly Category LivingLog_Sync    = new Category() { Id = 10, Name = "LivingLog_Sync"    };
        public static readonly Category LivingLog_Exit    = new Category() { Id = 11, Name = "LivingLog_Exit"    };

        public static readonly Category Mouse_Move        = new Category() { Id = 1, Name = "Mouse_Move"        };
        public static readonly Category Mouse_Down        = new Category() { Id = 2, Name = "Mouse_Down"        };
        public static readonly Category Mouse_Up          = new Category() { Id = 3, Name = "Mouse_Up"          };
        public static readonly Category Mouse_Click       = new Category() { Id = 4, Name = "Mouse_Click"       };
        public static readonly Category Mouse_DoubleClick = new Category() { Id = 5, Name = "Mouse_DoubleClick" };
        public static readonly Category Mouse_Wheel       = new Category() { Id = 6, Name = "Mouse_Wheel"       };

        public static readonly Category Keyboard_KeyDown  = new Category() { Id = 7, Name = "Keyboard_KeyDown"  };
        public static readonly Category Keyboard_KeyUp    = new Category() { Id = 8, Name = "Keyboard_KeyUp"    };
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

            Activity.SetParser(LivingLog_Startup, LivingLogger.SyncData.TryParse);
            Activity.SetParser(LivingLog_Sync,    LivingLogger.SyncData.TryParse);
            Activity.SetParser(LivingLog_Exit,    LivingLogger.SyncData.TryParse);

            Activity.SetParser(Mouse_Move,        MouseLogger.MouseMoveData.TryParse);
            Activity.SetParser(Mouse_Down,        MouseLogger.MouseButtonData.TryParse);
            Activity.SetParser(Mouse_Up,          MouseLogger.MouseButtonData.TryParse);
            Activity.SetParser(Mouse_Click,       MouseLogger.MouseButtonData.TryParse);
            Activity.SetParser(Mouse_DoubleClick, MouseLogger.MouseButtonData.TryParse);
            Activity.SetParser(Mouse_Wheel,       MouseLogger.MouseWheelData.TryParse);

            Activity.SetParser(Keyboard_KeyDown,  KeyboardLogger.KeyboardKeyData.TryParse);
            Activity.SetParser(Keyboard_KeyUp,    KeyboardLogger.KeyboardKeyData.TryParse);
            Activity.SetParser(Keyboard_KeyPress, KeyboardLogger.KeyboardPressData.TryParse);
        }

        public static Category get(int id)
        {
            Category result;
            if (!categories.TryGetValue(id, out result)) return null;
            return result;
        }
    }
}
