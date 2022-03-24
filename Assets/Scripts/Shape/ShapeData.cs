using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShapeType
{
    Cube, Bomb, Rocket, Disco, Box, Empty, Box2, Box3
}

public enum ShapeColor
{
    Blue, Red, Green, None
}

[CreateAssetMenu(fileName = "New Shape", menuName = "Shape Data", order = 51)]

public class ShapeData : ScriptableObject
{
    [SerializeField]
    private ShapeType _shapeType;

    [SerializeField]
    private ShapeColor _shapeColor;

    [SerializeField]
    private Sprite _sprite;

    [SerializeField] 
    private GameObject _explodeEffect;

    [SerializeField]
    private bool _isShiftable;

    [SerializeField]
    private bool _isGoalShape;

    public ShapeType ShapeType
    {
        get
        {
            return _shapeType;
        }
    }

    public ShapeColor ShapeColor
    {
        get
        {
            return _shapeColor;
        }
    }

    public Sprite Sprite
    {
        get
        {
            return _sprite;
        }
    }

    public GameObject ExplodeEffect
    {
        get
        {
            return _explodeEffect;
        }
    }

    public bool IsShiftable
    {
        get
        {
            return _isShiftable;
        }
    }
    
    public bool IsGoalShape
    {
        get
        {
            return _isGoalShape;
        }
    }
}
