using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class SelectTile : MonoBehaviour, IPointerClickHandler
{
    public List<Sprite> sprites;
    public GameObject highlight;
    public GameObject currentHighlight;
    public GameObject selectedTile;

    public GameObject warningText;

    public int[] saveArray;

    public List<GameObject> tiles;

    Dictionary <int, int> tilesDict = new Dictionary <int, int>();

    public TMP_InputField levelNumber;

    int levelNumberToPass;
    int movesToPass;

    public TMP_Dropdown goal1Shape;
    public TMP_Dropdown goal2Shape;
    public TMP_Dropdown goal3Shape;

    public InputField goal1Amount;
    public InputField goal2Amount;
    public InputField goal3Amount;
    public InputField moves;



    void Start()
    {
        saveArray = new int[36];
        foreach (Transform child in transform)
        {
            tiles.Add(child.gameObject);
            tilesDict.Add(tiles.IndexOf(child.gameObject), 0);
        }
    }    

    public void OnPointerClick(PointerEventData eventData)
    {
        selectedTile = eventData.pointerCurrentRaycast.gameObject;
        if (selectedTile.tag == "editorTile")
        {
            if (currentHighlight != null)
            {
                Destroy(currentHighlight);
            }
            GameObject _highlight = Instantiate(highlight, selectedTile.transform);
            currentHighlight = _highlight;
        }
    }

    public void SaveLevel()
    {
        SaveLoadManager slm = new SaveLoadManager();

        //To Write
        int[] shapesArray = saveArray;

        Goal[] goalsArray = new Goal[3];

        Goal denemeGoal = new Goal(GetGoalType(1), GetGoalColor(1), GetGoalAmount(1));
        Goal denemeGoal2 = new Goal(GetGoalType(2), GetGoalColor(2), GetGoalAmount(2));
        Goal denemeGoal3 = new Goal(GetGoalType(3), GetGoalColor(3), GetGoalAmount(3));

        goalsArray[0] = denemeGoal;
        goalsArray[1] = denemeGoal2;
        goalsArray[2] = denemeGoal3;

        Level level = new Level
        {
            level = Int32.Parse(levelNumber.text),
            row = 6,
            col = 6,
            moves = Int32.Parse(moves.text),
            shapesArray = shapesArray,
            goalsArray = goalsArray
        };

        slm.WriteLevelData(level);
        Debug.Log("Level " + level.level + " saved!");
    }


    private ShapeType GetGoalType(int goal)
    {
        if (goal == 1)
        {
            if (goal1Shape.value >= 0 && goal1Shape.value < 3)
            {
                return ShapeType.Cube;
            }
            else if (goal1Shape.value == 3)
            {
                return ShapeType.Box;
            }
            else return ShapeType.Cube;
        }
        else if (goal == 2)
        {
            if (goal2Shape.value >= 0 && goal2Shape.value < 3)
            {
                
                return ShapeType.Cube;
            }
            else if (goal2Shape.value == 3)
            {
                return ShapeType.Box;
            }
            else return ShapeType.Cube;
        }
        else if (goal == 3)
        {
            if (goal3Shape.value >= 0 && goal3Shape.value < 3)
            {
                return ShapeType.Cube;
            }
            else if (goal3Shape.value == 3)
            {
                return ShapeType.Box;
            }
            else return ShapeType.Cube;
        }
        else return ShapeType.Cube;
    }

    private ShapeColor GetGoalColor(int goal)
    {
        if (goal == 1)
        {
            if (goal1Shape.value == 0)
            {
                return ShapeColor.Red;
            }
            else if (goal1Shape.value == 1)
            {
                return ShapeColor.Green;
            }
            else if (goal1Shape.value == 2)
            {
                return ShapeColor.Blue;
            }
            else return ShapeColor.None;
        }
        else if (goal == 2)
        {
            if (goal2Shape.value == 0)
            {
                return ShapeColor.Red;
            }
            else if (goal2Shape.value == 1)
            {
                return ShapeColor.Green;
            }
            else if (goal2Shape.value == 2)
            {
                return ShapeColor.Blue;
            }
            else return ShapeColor.None;
        }
        else if (goal == 3)
        {
            if (goal3Shape.value == 0)
            {
                return ShapeColor.Red;
            }
            else if (goal3Shape.value == 1)
            {
                return ShapeColor.Green;
            }
            else if (goal3Shape.value == 2)
            {
                return ShapeColor.Blue;
            }
            else return ShapeColor.None;
        }
        else return ShapeColor.None;        
    }


    private int GetGoalAmount(int goal)
    {
        if (goal == 1)
        {
            return Int32.Parse(goal1Amount.text);
        }
        else if (goal == 2)
        {
            return Int32.Parse(goal2Amount.text);
        }
        else if (goal == 3)
        {
            return Int32.Parse(goal3Amount.text);
        }
        else return 0;
    }
  

    public void FillSaveArray()
    {
        int[,] tempArray = new int[6, 6];
        int index = 0;
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                tempArray[5 - i,j] = tilesDict[index];
                index++;
            }
        }

        index = 0;

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                saveArray[index] = tempArray[i,j];
                index++;
            }
        }

    }

    public void SaveButton()
    {
        if (levelNumber.text == "" || moves.text == "" || goal1Amount.text == "" || goal2Amount.text == "" || goal3Amount.text == "")
        {
            StartCoroutine(FieldWarning());
        }
        else
        {
            SetLevelNumber();
            SetMoves();
            FillSaveArray();
            SaveLevel();
            StartCoroutine(SavedText());
        }
    }

    private void SetLevelNumber()
    {
        try
        {
            levelNumberToPass = int.Parse(levelNumber.text);
        }
        catch (Exception ex)
        {
            Debug.Log("Please enter a valid number.");
        }
    }

    private void SetMoves()
    {
        try
        {
            movesToPass = int.Parse(moves.text);
        }
        catch (Exception ex)
        {
            Debug.Log("Please enter a valid number.");
        }
    }


    private IEnumerator FieldWarning()
    {
        warningText.GetComponent<Text>().text = "Please fill all fields.";
        warningText.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        warningText.SetActive(false);
    }

    private IEnumerator SavedText()
    {
        warningText.GetComponent<Text>().text = "Level saved!";
        warningText.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        warningText.SetActive(false);
    }

    public void PlaceRedCube()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[0];
            tilesDict[tiles.IndexOf(selectedTile)] = 3;
        }
    }
    public void PlaceGreenCube()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[1];
            tilesDict[tiles.IndexOf(selectedTile)] = 2;
        }
    }
    public void PlaceBlueCube()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[2];
            tilesDict[tiles.IndexOf(selectedTile)] = 1;
        }
    }
    public void PlaceCrate()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[3];
            tilesDict[tiles.IndexOf(selectedTile)] = 4;
        }
    }
    public void PlaceRocket()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[4];
            tilesDict[tiles.IndexOf(selectedTile)] = 6;
        }
    }
    public void PlaceBomb()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[5];
            tilesDict[tiles.IndexOf(selectedTile)] = 5;
        }
    }
    public void PlaceRedDisco()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[17];
            tilesDict[tiles.IndexOf(selectedTile)] = 9;
        }
    }
    public void PlaceGreenDisco()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[16];
            tilesDict[tiles.IndexOf(selectedTile)] = 8;
        }
    }
    public void PlaceBlueDisco()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[15];
            tilesDict[tiles.IndexOf(selectedTile)] = 7;
        }
    }

    public void PlaceBox2()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[18];
            tilesDict[tiles.IndexOf(selectedTile)] = 10;
        }
    }

    public void PlaceBox3()
    {
        if (selectedTile != null)
        {
            selectedTile.GetComponent<Image>().sprite = sprites[19];
            tilesDict[tiles.IndexOf(selectedTile)] = 11;
        }
    }
}
