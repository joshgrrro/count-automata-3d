using System;
using System.Collections;

/// <summary>
/// A class representing a 3D field of Cellular Automata.
/// </summary>
public class CountAutomata3D
{
    public readonly int width;
    public readonly int height;
    public readonly int depth;
    public bool[] cells { get; private set; }

    byte[] count;
    bool[] dirty;
    int cellCount;

    byte[] countBuffer;
    bool[] dirtyBuffer;

    int[,,] relativeIndeces = new int[3,3,3];

    /// <summary>
    /// Whether an edge cell should count non-existent cells as set or not set.
    /// </summary>
    public bool countBoundaries;

    /// <summary>
    /// The rules for the cellular automata.
    /// </summary>
    public bool[] rules;

    /// <summary>
    /// Creates and initializes a new Automata instance.
    /// </summary>
    public CountAutomata3D(int width, int height, int depth, bool[] rules, bool countBoundaries = false, int startChance = 50)
    {
        this.width = width;
        this.height = height;
        this.depth = depth;
        this.rules = rules;
        this.countBoundaries = countBoundaries;

        cellCount = width * height * depth;
        cells = new bool[cellCount];
        count = new byte[cellCount];
        dirty = new bool[cellCount];

        var random = new Random();
        for (var i = 0; i < cellCount; i++)
        {
            cells[i] = random.Next(100) < startChance;
            dirty[i] = true;
        }

        CalculateRelativeIndeces();
        InitialCount();
    }

    /// <summary>
    /// Runs a single update pass on the Automata.
    /// </summary>
    public void Update()
    {
        countBuffer = (byte[])count.Clone();
        dirtyBuffer = new bool[cellCount];

        // iterate through all cells
        var i = 0;
        for (var z = 0; z < depth; z++)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    // skip cells that are not dirty
                    if (!dirty[i])
                    {
                        i++;
                        continue;
                    }

                    // get the new state
                    bool newState = rules[count[i] + (cells[i] ? 27 : 0)];

                    // continue if no change
                    if (newState == cells[i])
                    {
                        i++;
                        continue;
                    }

                    // otherwise, change the value
                    cells[i] = newState;

                    // adjust neighbors and mark dirty
                    int _minx = x == 0 ? 1 : 0;
                    int _maxx = x == width - 1 ? 1 : 3;
                    int _miny = y == 0 ? 1 : 0;
                    int _maxy = y == height - 1 ? 1 : 3;
                    int _minz = z == 0 ? 1 : 0;
                    int _maxz = z == depth - 1 ? 1 : 3;
                    for (var _x = _minx; _x < _maxx; _x++)
                    {
                        for (var _y = _miny; _y < _maxy; _y++)
                        {
                            for (var _z = _minz; _z < _maxz; _z++)
                            {
                                if (_x == 0 && _y == 0 && _z == 0)
                                    continue;

                                if (newState)
                                {
                                    countBuffer[i + relativeIndeces[_x, _y, _z]]++;
                                }
                                else
                                {
                                    countBuffer[i + relativeIndeces[_x, _y, _z]]--;
                                }
                                dirtyBuffer[i + relativeIndeces[_x, _y, _z]] = true;
                            }
                        }
                    }

                    // increment the index
                    i++;
                }
            }
        }

        // set buffer arrays as next read arrays
        count = countBuffer;
        dirty = dirtyBuffer;
    }

    /// <summary>
    /// Initializes an update. Call this method before runnning any instances of PartialUpdate.
    /// </summary>
    public void InitializeUpdate()
    {
        // initialize the update arrays
        countBuffer = (byte[])count.Clone();
        dirtyBuffer = new bool[cellCount];
    }

    /// <summary>
    /// Finalizes an update. Call this method after all instances of PartialUpdate are complete.
    /// </summary>
    public void FinalizeUpdate()
    {
        // set buffer arrays as next read arrays
        count = countBuffer;
        dirty = dirtyBuffer;
    }

    /// <summary>
    /// Runs a partial update on the field of cells from index start (inclusive) to end (exclusive). Use for asnychronous updates.
    /// </summary>
    public void PartialUpdate(int start, int end)
    {
        // calculate coordinates
        var i = start;
        var zStart = i / (width * height);
        var yStart = (i - (zStart * width * height)) / width;
        var xStart = i - (zStart * width * height) - (yStart * width);

        // iterate through all cells
        for (var z = zStart; z < depth; z++)
        {
            for (var y = yStart; y < height; y++)
            {
                for (var x = xStart; x < width; x++)
                {
                    if (i >= end)
                        break;

                    // skip cells that are not dirty
                    if (!dirty[i])
                    {
                        i++;
                        continue;
                    }

                    // get a reference to the current cell state
                    //ref bool cellState = ref cells[i];

                    // get the new state
                    bool newState = rules[count[i] + (cells[i] ? 27 : 0)];

                    // continue if no change
                    if (newState == cells[i])
                    {
                        i++;
                        continue;
                    }

                    // otherwise, change the value
                    cells[i] = newState;

                    // adjust neighbors and mark dirty
                    int _minx = x == 0 ? 1 : 0;
                    int _maxx = x == width - 1 ? 1 : 3;
                    int _miny = y == 0 ? 1 : 0;
                    int _maxy = y == height - 1 ? 1 : 3;
                    int _minz = z == 0 ? 1 : 0;
                    int _maxz = z == depth - 1 ? 1 : 3;
                    for (var _x = _minx; _x < _maxx; _x++)
                    {
                        for (var _y = _miny; _y < _maxy; _y++)
                        {
                            for (var _z = _minz; _z < _maxz; _z++)
                            {
                                if (_x == 0 && _y == 0 && _z == 0)
                                    continue;

                                if (newState)
                                {
                                    countBuffer[i + relativeIndeces[_x, _y, _z]]++;
                                }
                                else
                                {
                                    countBuffer[i + relativeIndeces[_x, _y, _z]]--;
                                }
                                dirtyBuffer[i + relativeIndeces[_x, _y, _z]] = true;
                            }
                        }
                    }

                    // increment the index
                    i++;
                }

                xStart = 0;

                if (i >= end)
                    break;
            }

            yStart = 0;

            if (i >= end)
                break;
        }
    }

    private void InitialCount()
    {
        // iterate through all cells
        var i = 0;
        for (var z = 0; z < depth; z++)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    // count boundary cells
                    if (countBoundaries)
                    {
                        var border = 0;
                        if (x == 0 || x == width - 1)
                            border++;
                        if (y == 0 || y == height - 1)
                            border++;
                        if (z == 0 || z == depth - 1)
                            border++;

                        if (border > 0)
                            count[i] += 9;
                        if (border > 1)
                            count[i] += 6;
                        if (border > 2)
                            count[i] += 4;
                    }

                    // continue if cell is not set
                    if (!cells[i])
                    {
                        i++;
                        continue;
                    }

                    // otherwise, increment all neighbors
                    int _minx = x == 0 ? 1 : 0;
                    int _maxx = x == width - 1 ? 1 : 3;
                    int _miny = y == 0 ? 1 : 0;
                    int _maxy = y == height - 1 ? 1 : 3;
                    int _minz = z == 0 ? 1 : 0;
                    int _maxz = z == depth - 1 ? 1 : 3;
                    for (var _x = _minx; _x < _maxx; _x++)
                    {
                        for (var _y = _miny; _y < _maxy; _y++)
                        {
                            for (var _z = _minz; _z < _maxz; _z++)
                            {
                                if (_x == 0 && _y == 0 && _z == 0)
                                    continue;

                                count[i + relativeIndeces[_x, _y, _z]]++;
                            }
                        }
                    }

                    // increment the index
                    i++;
                }
            }
        }
    }

    private void CalculateRelativeIndeces()
    {
        for (var z = -1; z < 2; z++)
        {
            for (var y = -1; y < 2; y++)
            {
                for (var x = -1; x < 2; x++)
                {
                    relativeIndeces[x + 1, y + 1, z + 1] = x + (y * width) + (z * width * height);
                }
            }
        }
    }


}