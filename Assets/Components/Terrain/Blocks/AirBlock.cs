using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Antymology.Terrain
{
    /// <summary>
    /// The air type of block. Contains the internal data representing phermones in the air.
    /// </summary>
    public class AirBlock : AbstractBlock
    {

        #region Fields

        /// <summary>
        /// Statically held is visible.
        /// </summary>
        private static bool _isVisible = false;

        /// <summary>
        /// A dictionary representing the phermone deposits in the air. Each type of phermone gets it's own byte key, and each phermone type has a concentration.
        /// THIS CURRENTLY ONLY EXISTS AS A WAY OF SHOWING YOU HOW YOU CAN MANIPULATE THE BLOCKS.
        /// </summary>
        private Dictionary<byte, double> phermoneDeposits;

        #endregion

        #region Methods

        /// <summary>
        /// Air blocks are going to be invisible.
        /// </summary>
        public override bool isVisible()
        {
            return _isVisible;
        }

        /// <summary>
        /// Air blocks are invisible so asking for their tile map coordinate doesn't make sense.
        /// </summary>
        public override Vector2 tileMapCoordinate()
        {
            throw new Exception("An invisible tile cannot have a tile map coordinate.");
        }

        /// <summary>
        /// THIS CURRENTLY ONLY EXISTS AS A WAY OF SHOWING YOU WHATS POSSIBLE.
        /// </summary>
        /// <param name="neighbours"></param>
        public void Diffuse(AbstractBlock[] neighbours)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
