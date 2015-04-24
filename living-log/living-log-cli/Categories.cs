using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public static class Categories
    {
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

        public static Category get(int id)
        {
            var categories = new Category[] {
                 LivingLog_Startup,
                 LivingLog_Sync,
                 LivingLog_Exit,
                
                 Mouse_Move,
                 Mouse_Down,
                 Mouse_Up,
                 Mouse_Click,
                 Mouse_DoubleClick,
                 Mouse_Wheel,
                
                 Keyboard_KeyDown,
                 Keyboard_KeyUp,
                 Keyboard_KeyPress,
            };
            return categories.FirstOrDefault((c) => { return c.Id == id; });
        }

        public static Func<string, OutOfMemoryException IData>
    }
}
