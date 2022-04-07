using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public enum ShapeState
{
    Waiting, Shifting, Merging, Explode
}

public abstract class Shape : MonoBehaviour, IPointerDownHandler
{
    private const float TimeToWaitTurn = 0.03f;
    protected const float TimeToExpandOut = 0.2f;
    protected const float TimeToExpandIn = 0.1f;
    private const float TimeToTurnIntoBooster = 0.33f;
    private const float ExpandRateScale = 1.08f;
    private const float ExpandRatePosition = 0.2f;

    private const float TimeShiftDown = 0.07f;
    private const float TimeRefillShiftDown = 0.07f;
    private const float TimeBounce = 0.1f;
    private const float BounceAmount = 0.035f;


    public ShapeData _shapeData;
    public ShapeState _shapeState;

    public int _row;
    public int _col;

    protected List<Shape> _adjacentShapes;
    protected SpriteRenderer _spriteRenderer;

    private Sequence _shiftDownSequence;

    List<int> columns = new List<int>();


    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public abstract void Explode();

    public abstract void Merge();

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        BoardManager.Instance.SetIsMergesFound(false);
    }

    public virtual void SetShapeData(ShapeData shapeData, int row, int col)
    {
        this._row = row;
        this._col = col;
        _shapeData = shapeData;
        _spriteRenderer.sprite = _shapeData.Sprite;
        _spriteRenderer.sortingOrder = row + 2;
    }

    public void ReverseShapeSprite()
    {
        _spriteRenderer.sprite = _shapeData.Sprite;
    }

    public virtual void SetMergeSprite(int count) { }

    public virtual void FindAdjacentShapes(bool isThisClickedShape, List<Shape> adjacentShapes, List<Shape> adjacentGoals)
    {
        int rows = BoardManager.Instance.GetRowCount();
        int columns = BoardManager.Instance.GetColumnCount();

        if (isThisClickedShape)
            adjacentShapes.Add(this);

        _FindAdjacentShapes(_row, _col + 1, columns, false, adjacentShapes, adjacentGoals);
        _FindAdjacentShapes(_row, _col - 1, columns, false, adjacentShapes, adjacentGoals);
        _FindAdjacentShapes(_row + 1, _col, rows, true, adjacentShapes, adjacentGoals);
        _FindAdjacentShapes(_row - 1, _col, rows, true, adjacentShapes, adjacentGoals);
    }

    private void _FindAdjacentShapes(int row, int col, int constraint, bool isRowChanging, List<Shape> adjacentShapes, List<Shape> adjacentGoals)
    {
        Shape[,] shapeMatrix = BoardManager.Instance.GetShapeMatrix();

        int temp = isRowChanging ? row : col;

        if (temp < constraint && temp >= 0)
        {
            if (shapeMatrix[row, col] != null && !BoardManager.Instance.IsShapeCheckedBefore(adjacentShapes, shapeMatrix[row, col]) &&
                shapeMatrix[row, col]._shapeData.ShapeType == _shapeData.ShapeType &&
                shapeMatrix[row, col]._shapeData.ShapeColor == _shapeData.ShapeColor)
            {
                adjacentShapes.Add(shapeMatrix[row, col]);
                shapeMatrix[row, col].FindAdjacentShapes(false, adjacentShapes, adjacentGoals);
            }
            else if (shapeMatrix[row, col] != null &&
                    adjacentGoals != null &&
                    !BoardManager.Instance.IsShapeCheckedBefore(adjacentGoals, shapeMatrix[row, col]) &&
                    shapeMatrix[row, col]._shapeData.IsGoalShape)
            {
                adjacentGoals.Add(shapeMatrix[row, col]);
            }
        }
    }

    public void MoveToMergePoint(int row, int col)
    {
        Vector2 offset = _spriteRenderer.bounds.size;
        _shapeState = ShapeState.Merging;
        _spriteRenderer.sortingOrder = 98;

        int directionX = col - _col;
        int directionY = row - _row;

        float posX = transform.position.x + directionX * offset.x;
        float posY = transform.position.y + directionY * offset.y;

        float expandX = transform.position.x + ExpandRatePosition * offset.x * -1 * directionX;
        float expandY = transform.position.y + ExpandRatePosition * offset.y * -1 * directionY;

        float localScaleX = transform.localScale.x;
        float localScaleY = transform.localScale.y;

        transform.DOMove(new Vector3(expandX, expandY), TimeToExpandOut).
            SetEase(Ease.OutSine).
            OnComplete(() =>
            {
                transform.DOMove(new Vector3(posX, posY), TimeToExpandIn).OnComplete(() =>
                {
                    if (!(row == _row && col == _col))  // Bura de?i?cek
                        BoardManager.Instance.DestroyShape(this);

                    _shapeState = ShapeState.Waiting;
                });
            });

        transform.DOScale(new Vector3(transform.localScale.x * ExpandRateScale, transform.localScale.y * ExpandRateScale), TimeToExpandOut).SetEase(Ease.OutSine).OnComplete(() =>
        {
            transform.DOScale(new Vector3(localScaleX, localScaleY), TimeToExpandIn);
        });
    }

    #region Shift Down

    public void ShiftDown(bool isForRefill = false)
    {
        StartCoroutine(WaitForShiftDown(isForRefill));
    }

    private IEnumerator WaitForShiftDown(bool isForRefill)
    {
        Debug.Log("Waiting for a tile to end shifting.");
        //wait for other tiles to done with shifting
        yield return new WaitUntil(() => BoardManager.Instance.isShiftingATile == false);

        Debug.Log("Done waiting, starting to shift.");

        //take the turn of shifting
        BoardManager.Instance.isShiftingATile = true;

        //start shifting down
        ShiftDownAfterWait(isForRefill);
    }

    private void ShiftDownAfterWait(bool isForRefill)
    {
        //tile is not shiftable
        if (!_shapeData.IsShiftable)
        {
            BoardManager.Instance.isShiftingATile = false;

            return;
        }

        int rowToShift = isForRefill ? FindEmptyRow(BoardManager.Instance.GetRowCount() - 1) : FindEmptyRow(_row);

        HandleShiftDown(rowToShift, isForRefill);
    }

    private int FindEmptyRow(int rowIndex)
    {
        Shape[,] shapeMatrix = BoardManager.Instance.GetShapeMatrix();
        int rowToShift = -1;

        for (int i = rowIndex; i >= 0; i--)
            if (shapeMatrix[i, _col] == null)
                rowToShift = i;
            else if (shapeMatrix[i, _col]._shapeData.IsShiftable == false)
                break;

        return rowToShift;
    }

    private void HandleShiftDown(int rowToShift, bool isForRefill = false)
    {
        //row is out of bounds
        if (rowToShift == -1)
        {

            return;
        }

        Shape[,] shapeMatrix = BoardManager.Instance.GetShapeMatrix();

        //assign this shape to the matrix
        shapeMatrix[rowToShift, _col] = this;

        if (!isForRefill)
        {
            shapeMatrix[_row, _col] = null;
        }

        float temp = isForRefill ? TimeRefillShiftDown : TimeShiftDown;
        Shift(rowToShift, temp);
    }

    private void Shift(int rowToShift, float shiftDownTime)
    {
        //shape is already shifting
        if (_shapeState == ShapeState.Shifting)
        {
            //stop the old animation
            _shiftDownSequence.Kill();

            //find where this shape is at
            _row = FindCurrentRow();
        }

        //shape is not already shifting
        else
        {
            _shapeState = ShapeState.Shifting;
        }

        Vector2 offset = _spriteRenderer.bounds.size;

        float posToShift = offset.y * rowToShift - (rowToShift * 0.08f);

        //animate to position
        ShiftAnimation(posToShift, shiftDownTime, rowToShift);

        //set row to new row
        _row = rowToShift;
        _spriteRenderer.sortingOrder = _row + 1;
    }

    private void ShiftAnimation(float posToShift, float shiftDownTime, int rowToShift)
    {
        //create a new sequence
        _shiftDownSequence = DOTween.Sequence();

        //calculate the shift amount
        float shiftAmount = _row - rowToShift;

        _shiftDownSequence.Append(transform.DOLocalMoveY(posToShift, shiftDownTime * shiftAmount)
                           .SetEase(Ease.InQuad))
                           .Append(BounceShape(posToShift, shiftAmount))
                           .OnComplete(() =>
                           {
                               _shapeState = ShapeState.Waiting;
                               BoardManager.Instance.FindMerges();

                               //start shifting diagonally if possible
                               ShiftDiagonalAfterWait();
                           });
    }

    private int FindCurrentRow()
    {
        int currentRow;
        Vector2 offset = _spriteRenderer.bounds.size;
        currentRow = Mathf.RoundToInt(transform.localPosition.y / offset.y);
        return currentRow;
    }
    
    private int FindCurrentCol()
    {
        int currentCol;
        Vector2 offset = _spriteRenderer.bounds.size;
        currentCol = Mathf.RoundToInt(transform.localPosition.y / offset.x);
        return currentCol;
    }

    private Tween BounceShape(float posToShift, float shiftAmount)
    {
        return transform.DOLocalMoveY(posToShift + BounceAmount * shiftAmount, TimeBounce).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo);
    }

    #endregion

    #region Shift Diagonal
    /*
    public void ShiftDiagonal()
    {
        StartCoroutine(WaitForShiftDiagonal());
    }
    private IEnumerator WaitForShiftDiagonal()
    {
        yield return new WaitUntil(() => BoardManager.Instance.isShiftingATile == false);
        BoardManager.Instance.isShiftingATile = true;

        ShiftDiagonalAfterWait();
    }
    */
    private void ShiftDiagonalAfterWait()
    {

        //this block is not shiftable
        if (!_shapeData.IsShiftable)
        {
            //don't shift
            BoardManager.Instance.isShiftingATile = false;
            return;
        }

        Shape[,] shapeMatrix = BoardManager.Instance.GetShapeMatrix();

        //find the diagonal tiles this shape would shift to
        int downRow = _row - 1;
        int rightCol = _col + 1;
        int leftCol = _col - 1;

        Debug.Log("Matrix Dimensions: " + shapeMatrix.GetLength(0) + ", " + shapeMatrix.GetLength(1));
        Debug.Log("Trying shift diagonal to: (" + downRow + ", " + leftCol + ") or (" + downRow + ", " + rightCol + ").");
        //all of these tiles are out of bounds of the matrix
        if (downRow < 0 || downRow > shapeMatrix.GetLength(0))
        {
            BoardManager.Instance.isShiftingATile = false;

            return;
        }

        //the tile at down left is empty
        if (leftCol >= 0 && leftCol < shapeMatrix.GetLength(1) && shapeMatrix[downRow, leftCol] == null)
        {
            //save this column to shift down above this tile when diagonal shift is done
            columns.Clear();
            columns.Add(_col);

            //nullify the old place and set this tile into the new place
            shapeMatrix[_row, _col] = null;

            shapeMatrix[downRow, leftCol] = this;

            ShiftDiagonalTo(downRow, leftCol);

        }
        else if (rightCol >= 0 && rightCol < shapeMatrix.GetLength(1) && shapeMatrix[downRow, rightCol] == null)
        {
            //save this column to shift down above this tile when diagonal shift is done
            columns.Clear();
            columns.Add(_col);

            //nullify the old place and set this tile into the new place
            shapeMatrix[_row, _col] = null;
            shapeMatrix[downRow, rightCol] = this;

            ShiftDiagonalTo(downRow, rightCol);

        }
        else
        {
            //stop shifting
            BoardManager.Instance.isShiftingATile = false;
        }

    }

    private void ShiftDiagonalTo(int row, int col)
    {
        Vector2 offset = _spriteRenderer.bounds.size;

        //calculate the position to shift
        float xWordlPos = offset.x * col;
        float yWorldPos = offset.y * row - (row * 0.08f);
        Vector2 positionToShift = new Vector2(xWordlPos, yWorldPos);

        AnimateShift(positionToShift, TimeShiftDown);

        _row = row;
        _spriteRenderer.sortingOrder = _row + 1;

        _col = col;
    }

    private void AnimateShift(Vector2 positionToAnimate, float shiftTime)
    {
        
        //not shifting currently
        if (_shapeState != ShapeState.Shifting)
        {
            _shapeState = ShapeState.Shifting;

            //create a new sequence
            _shiftDownSequence = DOTween.Sequence();

        }
        

        float shiftAmount = (new Vector2(_col, _row) - positionToAnimate).magnitude;

        _shiftDownSequence.Append(transform.DOLocalMove(positionToAnimate, shiftTime * shiftAmount)
                           .SetEase(Ease.InQuad))
                           //.Append(BounceShape(positionToAnimate, shiftAmount))
                           .OnComplete(() =>
                           {
                               _shapeState = ShapeState.Waiting;

                               BoardManager.Instance.isShiftingATile = false;

                               BoardManager.Instance.StartShiftDown(columns);

                               _shiftDownSequence.Kill();

                           });
    }

    /*
    private void DiagonalBounceShape(Vector2 position, float shiftAmount)
    {
        return transform.DOLocalMove(position + BounceAmount * shiftAmount, TimeBounce).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo);
    }
    */

    #endregion

}
