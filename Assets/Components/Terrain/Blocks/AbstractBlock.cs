using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractBlock
{
    /// <summary>
    /// The texture map coordinates of this block.
    /// </summary>
    public abstract Vector2 tileMapCoordinate();

    /// <summary>
    /// If the block is visible or not.
    /// </summary>
    public abstract bool isVisible();

    /// <summary>
    /// The woorld x coordinate of this block.
    /// </summary>
    public int worldXCoordinate;

    /// <summary>
    /// The world y coordinate of this block.
    /// </summary>
    public int worldYCoordinate;

    /// <summary>
    /// The world z coordinate of this block.
    /// </summary>
    public int worldZCoordinate;
}
