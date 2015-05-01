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

        public static Category LivingLog_Startup = new Category() { Id = 0, Name = "LivingLog_Startup" };
        public static Category LivingLog_Sync = new Category() { Id = 10, Name = "LivingLog_Sync" };
        public static Category LivingLog_Exit = new Category() { Id = 11, Name = "LivingLog_Exit" };

        public static Category Mouse_Move = new Category() { Id = 1, Name = "Mouse_Move" };
        public static Category Mouse_Down = new Category() { Id = 2, Name = "Mouse_Down" };
        public static Category Mouse_Up = new Category() { Id = 3, Name = "Mouse_Up" };
        public static Category Mouse_Click = new Category() { Id = 4, Name = "Mouse_Click" };
        public static Category Mouse_DoubleClick = new Category() { Id = 5, Name = "Mouse_DoubleClick" };
        public static Category Mouse_Wheel = new Category() { Id = 6, Name = "Mouse_Wheel" };

        public static Category Keyboard_KeyDown = new Category() { Id = 7, Name = "Keyboard_KeyDown" };
        public static Category Keyboard_KeyUp = new Category() { Id = 8, Name = "Keyboard_KeyUp" };
        public static Category Keyboard_KeyPress = new Category() { Id = 9, Name = "Keyboard_KeyPress" };

        private static Dictionary<int, Category> categories;
        private static Dictionary<Category, TryParseIData> parsers;

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

            parsers = new Dictionary<Category, TryParseIData>()
            {
                { LivingLog_Startup , LivingLogger.SyncData.TryParse },
                { LivingLog_Sync    , LivingLogger.SyncData.TryParse },
                { LivingLog_Exit    , LivingLogger.SyncData.TryParse },
                                                           
                { Mouse_Move        , MouseLogger.MouseMoveData.TryParse   },
                { Mouse_Down        , MouseLogger.MouseButtonData.TryParse },
                { Mouse_Up          , MouseLogger.MouseButtonData.TryParse },
                { Mouse_Click       , MouseLogger.MouseButtonData.TryParse },
                { Mouse_DoubleClick , MouseLogger.MouseButtonData.TryParse },
                { Mouse_Wheel       , MouseLogger.MouseWheelData.TryParse  },
                                                           
                { Keyboard_KeyDown  , KeyboardLogger.KeyboardKeyData.TryParse   },
                { Keyboard_KeyUp    , KeyboardLogger.KeyboardKeyData.TryParse   },
                { Keyboard_KeyPress , KeyboardLogger.KeyboardPressData.TryParse },
            };
        }

        public static Category get(int id)
        {
            Category result;
            if (!categories.TryGetValue(id, out result)) return null;
            return result;
        }

        public delegate bool TryParseIData(string s, out IData result);

        public static bool NullParser(string s, out IData result) { result = null; return false; }
        public static TryParseIData parser(Category c)
        {
            TryParseIData parser;
            if (!parsers.TryGetValue(c, out parser)) parser = NullParser;    
            return parser;
        }
    }
}
