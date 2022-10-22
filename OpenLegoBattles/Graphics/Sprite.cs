using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenLegoBattles.Graphics
{
    public class Sprite
    {
        #region Properties
        /// <summary> The <see cref="Spritesheet"/> that this sprite exists on. </summary>
        public Spritesheet Spritesheet { get; }

        /// <summary> The 1-dimensional index of this sprite. </summary>
        public ushort Index { get; }

        /// <summary> The tint colour of this sprite, defaulting to <see cref="Color.White"/>. </summary>
        public Color Colour { get; }

        /// <summary> Is <c>true</c> if this sprite is missing its <see cref="Spritesheet"/> (and is hence empty); otherwise <c>false</c>. </summary>
        public bool IsEmpty => Spritesheet == null;
        #endregion

        #region Presets
        public static Sprite Empty { get; } = new Sprite(null, 0, null);
        #endregion

        #region Constructors
        public Sprite(Spritesheet spritesheet, int index, Color? colour = null)
        {
            Spritesheet = spritesheet;
            Index = spritesheet == null ? (ushort)0 : (ushort)index;
            Colour = spritesheet != null && colour != null ? colour.Value : Color.White;
        }
        #endregion

        #region Source Functions
        /// <summary> Calculates the source rectangle of this sprite on its <see cref="Spritesheet"/>. </summary>
        /// <returns> The source rectangle of this sprite on its <see cref="Spritesheet"/>. </returns>
        public Rectangle CalculateSourceRectangle() => Spritesheet?.CalculateSourceRectangle(Index) ?? Rectangle.Empty;
        #endregion

        #region String Functions
        public override string ToString() => $"Sprite on spritesheet {Spritesheet} with index of {Index} and colour {Colour}.";
        #endregion
    }
}
