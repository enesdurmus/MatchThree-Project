using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box2 : Shape
{
    public override void Explode()
    {
        Shape shape = gameObject.AddComponent<Box1>();
        shape.SetShapeData(BoardManager.Instance.GetShapeData(ShapeType.Box2), _row, _col);
        BoardManager.Instance.ReloadShapeToList(shape, _row, _col);
        Destroy(GetComponent<Box2>());
    }

    public override void Merge()
    {
        throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
