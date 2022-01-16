using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Shared.World
{
    public class Level
    {
        public static Level TEMPLE_OF_FLORETO_WEST_ENTRANCE;

        public static void InitLevels()
        {
            TEMPLE_OF_FLORETO_WEST_ENTRANCE = new Level();
        }

        public Level()
        {

        }
    }
}
