using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

public enum BoosterMerge
{
    BigLightBall, LightBallWithBomb, LightBallWithRocket, BigBomb, DoubleRocket, BombWithRocket, None
}

public abstract class Booster : Shape
{
    private const float TimeToShiftAfterBombExplosion = 0.3f;
    private const float TimeToShiftAfterRocketExplosion = 0.3f;
    private const float TimeToShiftAfterBombRocketExplosion = 1.5f;
    private const float TimeToShiftAfterDoubleRocketExplosion = 0.6f;

    protected BoosterMerge _boosterMerge = BoosterMerge.None;
    protected List<Shape> _adjacentBoosters;

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (BoardManager.Instance.isMovesLeft() &&
            BoardManager.Instance.GetGameState() == GameState.Ready &&
            _shapeState == ShapeState.Waiting)
        {
            BoardManager.Instance.DecreaseRemainingMoves();
            _boosterMerge = GetBoosterMerge();
            HandleBoosterExplosion();
            //StartCoroutine(BoardManager.Instance.StartShiftDownTrigger());
        }
    }

    private void HandleBoosterExplosion()
    {
        switch (_boosterMerge)
        {
            case BoosterMerge.BigLightBall:
                BoardManager.Instance.SetGameState(GameState.BigDiscoExplosion);
                HandleBigLightBall();
                StartCoroutine(WaitStartShift(0.7f + TimeToExpandIn + TimeToExpandOut));
                BoardManager.Instance.DelayedShiftDown(new List<int> { 0, 1, 2, 3, 4, 5 }, 0.7f + TimeToExpandIn + TimeToExpandOut);
                break;
            case BoosterMerge.LightBallWithBomb:
                BoardManager.Instance.SetGameState(GameState.DiscoBombExplosion);
                HandleLightBallWithBomb();
                break;
            case BoosterMerge.LightBallWithRocket:
                BoardManager.Instance.SetGameState(GameState.DiscoRocketExplosion);
                HandleLightBallWithRocket();
                break;
            case BoosterMerge.BigBomb:
                BoardManager.Instance.SetGameState(GameState.BigBombExplosion);
                HandleBigBomb();
                StartCoroutine(WaitStartShift(2f));
                break;
            case BoosterMerge.BombWithRocket:
                BoardManager.Instance.SetGameState(GameState.RocketBombExplosion);
                StartCoroutine(BoardManager.Instance.StartShiftDownTrigger());

                HandleBombWithRocket(); 
                StartCoroutine(WaitStartShift(TimeToShiftAfterBombRocketExplosion));
                break;
            case BoosterMerge.DoubleRocket:
                BoardManager.Instance.SetGameState(GameState.DoubleRocket); 
                StartCoroutine(BoardManager.Instance.StartShiftDownTrigger());
                HandleDoubleRocket();
                StartCoroutine(WaitStartShift(TimeToShiftAfterDoubleRocketExplosion));
                break;
            case BoosterMerge.None:
                Explode();
                StartCoroutine(BoardManager.Instance.StartShiftDownTrigger());

                if (GetType() == typeof(Rocket))
                    StartCoroutine(WaitStartShift(TimeToShiftAfterRocketExplosion));
                else if(GetType() == typeof(Bomb))
                    StartCoroutine(WaitStartShift(TimeToShiftAfterBombExplosion));
                break;
        }
    }

    protected IEnumerator WaitStartShift(float timeToShift, GameState source = GameState.Ready)
    {
        yield return new WaitForSeconds(timeToShift);
        BoardManager.Instance.SetGameState(GameState.Ready, source);
        Destroy(gameObject, 0.75f);
    }

    private void SetAdjacentBoosters(List<Shape> adjacentBoosters)
    {
        _adjacentBoosters = adjacentBoosters;
    }

    public override void FindAdjacentShapes(bool isThisClickedShape, List<Shape> adjacentShapes, List<Shape> adjacentGoals = null)
    {
        int rows = BoardManager.Instance.GetRowCount();
        int columns = BoardManager.Instance.GetColumnCount();

        if (isThisClickedShape)
            adjacentShapes.Add(this);

        _FindAdjacentShapes(_row, _col + 1, columns, false, adjacentShapes);
        _FindAdjacentShapes(_row, _col - 1, columns, false, adjacentShapes);
        _FindAdjacentShapes(_row + 1, _col, rows, true, adjacentShapes);
        _FindAdjacentShapes(_row - 1, _col, rows, true, adjacentShapes);
    }

    private void _FindAdjacentShapes(int row, int col, int constraint, bool isRowChanging, List<Shape> adjacentShapes)
    {
        Shape[,] shapeMatrix = BoardManager.Instance.GetShapeMatrix();

        int temp = isRowChanging ? row : col;

        if (temp < constraint && temp >= 0)
        {
            if (shapeMatrix[row, col] != null &&
                !BoardManager.Instance.IsShapeCheckedBefore(adjacentShapes, shapeMatrix[row, col]) &&
                shapeMatrix[row, col]._shapeData.ShapeType != ShapeType.Cube &&
                shapeMatrix[row, col]._shapeData.ShapeType != ShapeType.Box &&
                shapeMatrix[row, col]._shapeData.ShapeType != ShapeType.Box2 &&
                    shapeMatrix[row, col]._shapeData.ShapeType != ShapeType.Box3)
            {
                adjacentShapes.Add(shapeMatrix[row, col]);
                shapeMatrix[row, col].FindAdjacentShapes(false, adjacentShapes, null);
            }
        }
    }

    public BoosterMerge GetBoosterMerge()
    {
        _adjacentBoosters = new List<Shape>();
        FindAdjacentShapes(true, _adjacentBoosters);

        if (GetSpecificBoosterCount(ShapeType.Disco) > 1)
            return BoosterMerge.BigLightBall;
        else if (GetIsBoosterExist(ShapeType.Disco) && GetIsBoosterExist(ShapeType.Bomb))
            return BoosterMerge.LightBallWithBomb;
        else if (GetIsBoosterExist(ShapeType.Disco) && GetIsBoosterExist(ShapeType.Rocket))
            return BoosterMerge.LightBallWithRocket;
        else if (GetSpecificBoosterCount(ShapeType.Bomb) > 1)
            return BoosterMerge.BigBomb;
        else if (GetIsBoosterExist(ShapeType.Bomb) && GetIsBoosterExist(ShapeType.Rocket))
            return BoosterMerge.BombWithRocket;
        else if (GetSpecificBoosterCount(ShapeType.Rocket) > 1)
            return BoosterMerge.DoubleRocket;

        return BoosterMerge.None;
    }

    private bool GetIsBoosterExist(ShapeType shapeType)
    {
        return _adjacentBoosters.Exists(booster => booster._shapeData.ShapeType == shapeType);
    }

    private int GetSpecificBoosterCount(ShapeType shapeType)
    {
        return _adjacentBoosters.Count(booster => booster._shapeData.ShapeType == shapeType);
    }

    #region Handle Booster Operations

    private void HandleBigLightBall()
    {
        Disco disco;
        if (GetType() != typeof(Disco))
        {
            disco = gameObject.AddComponent<Disco>();
            disco.SetAdjacentBoosters(_adjacentBoosters);
            disco.SetShapeData(BoardManager.Instance.GetShapeData(ShapeType.Disco, ShapeColor.Blue), _row, _col, true);
        }
        else
            disco = (Disco)this;

        disco.Merge();
        StartCoroutine(disco.WaitForBigLightBall());
    }

    private void HandleLightBallWithBomb()
    {
        Disco disco;

        if (GetType() != typeof(Disco))
        {
            disco = gameObject.AddComponent<Disco>();
            disco.SetAdjacentBoosters(_adjacentBoosters);
            disco.SetShapeData(BoardManager.Instance.GetShapeData(ShapeType.Disco, ShapeColor.Blue), _row, _col, true);
        }
        else
            disco = (Disco)this;

        disco._spriteRenderer.enabled = false;
        disco.Merge();
        disco._shapeState = ShapeState.Explode;
        StartCoroutine(disco.WaitForLightBallWithBomb());
    }

    private void HandleLightBallWithRocket()
    {
        Disco disco;

        if (GetType() != typeof(Disco))
        {
            disco = gameObject.AddComponent<Disco>();
            disco.SetAdjacentBoosters(_adjacentBoosters);
            disco.SetShapeData(BoardManager.Instance.GetShapeData(ShapeType.Disco, ShapeColor.Blue), _row, _col, true);
        }
        else
        {
            disco = (Disco)this;
        }

        disco._spriteRenderer.enabled = false;
        disco.Merge();
        disco._shapeState = ShapeState.Explode;
        StartCoroutine(disco.WaitForLightBallWithRocket());
    }

    private void HandleBigBomb()
    {
        Bomb bomb;
        if (GetType() != typeof(Bomb))
        {
            bomb = gameObject.AddComponent<Bomb>();
            bomb.SetAdjacentBoosters(_adjacentBoosters);
            bomb.SetShapeData(BoardManager.Instance.GetShapeData(ShapeType.Bomb, ShapeColor.None), _row, _col, true);
        }
        else
            bomb = (Bomb)this;

        bomb._spriteRenderer.enabled = false;
        bomb.Merge();
        StartCoroutine(bomb.WaitForExplode5x5());
    }

    private void HandleBombWithRocket()
    {
        Rocket rocket;
        if (GetType() != typeof(Rocket))
        {
            rocket = gameObject.AddComponent<Rocket>();
            rocket.SetAdjacentBoosters(_adjacentBoosters);
            rocket.SetShapeData(BoardManager.Instance.GetShapeData(ShapeType.Rocket, ShapeColor.None), _row, _col, true);
        }
        else
            rocket = (Rocket)this;

        rocket.Merge();
        StartCoroutine(rocket.WaitForExplodeRocketWithBomb());
    }

    private void HandleDoubleRocket()
    {
        Rocket rocket = (Rocket)this;
        rocket.Merge();
        StartCoroutine(rocket.WaitForExplodeDoubleRocket());
    }
}
    #endregion