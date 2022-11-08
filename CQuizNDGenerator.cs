using System;
using System.Collections.Generic;
using System.Linq;
using ApnaGamesQuizFSM;
using CodeStage.AntiCheat.Storage;
using Mapbox.Unity.Utilities;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using Random = System.Random;

public class CQuizNDGenerator : MonoBehaviour
{
    #region Variables
    public static CQuizNDGenerator Instance { get; private set; } //Singleton
    [HideInInspector]
    public int BufferCount;                //KEEP PUBLIC (required by playfab directive) Total number of questions to keep in a players buffer. 
    [SerializeField]
    private int _numberOfQuestions;        //Number of questions per round.
    [SerializeField]
    private int _totalQuestionCount;       //Total count of questions. Eventually, this number will be pulled from Nadia's CSV table's length
    [Tooltip("Manual Buffer override. DO NOT ENTER unless testing. Format: 1,2,3,4")]
    [SerializeField]
    private string _player1Buffer;         //Buffer of questions player1  has. When the buffer count is exceeded, starting few question numbers are removed.
    [Tooltip("Manual Buffer override. DO NOT ENTER unless testing. Format: 1,2,3,4")]
    [SerializeField]
    private string _player2Buffer;         //Buffer of questions player2 has. When the buffer count is exceeded, starting few question numbers are removed.

    //BELOW ARE ONLY FOR DEBUG PURPOSES
    [Tooltip("Assign textbox for testing purpose")]
    [SerializeField]
    private Text _p1Buffer;             //UI Text representation of Player 1's buffer.
    [Tooltip("Assign textbox for testing purpose")]
    [SerializeField]
    private Text  _p2Buffer;            //UI Text representation of Player 2's buffer.
    [Tooltip("Assign textbox for testing purpose")]
    [SerializeField]
    private Text _p1BufferCount;        //UI Text representation of Questions generated this round only.
    [Tooltip("Assign textbox for testing purpose")]
    [SerializeField]
    private Text _p2BufferCount;        //UI Text representation of Questions generated this round only.
    [Tooltip("Assign textbox for testing purpose")]
    [SerializeField]
    private Text _displayQuestions;     //UI Text representation of Questions generated this round only.
    #endregion
    
    #region PublicMethods

    void Start()
    {
        if (Instance != null && Instance != this) //Singleton
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        // Below ObscuredPrefs are being Hard Coded as per Trang, remove later.
        if (!ObscuredPrefs.HasKey("OtherModesBufferIndexMin"))
        {
            ObscuredPrefs.SetInt("OtherModesBufferIndexMin", 1);
        }
        if (!ObscuredPrefs.HasKey("OtherModesBufferIndexMax"))
        {
            ObscuredPrefs.SetInt("OtherModesBufferIndexMax", 169);
        }
        if (!ObscuredPrefs.HasKey("EventModeBufferIndexMin"))
        {
            ObscuredPrefs.SetInt("EventModeBufferIndexMin", 20001);    
        }
        if (!ObscuredPrefs.HasKey("EventModeBufferIndexMax"))
        {
            ObscuredPrefs.SetInt("EventModeBufferIndexMax", 20020);    
        }

        ObscuredPrefs.SetInt("EventModeBufferIndexMin", 20101);
        ObscuredPrefs.SetInt("EventModeBufferIndexMax", 20120);
    }
     //Debug Method called when button is pressed. Can remove later. Only used for testing purposes.
     public void DisplayNumbers()
    {
        int[] x = GenerateQuestions(_numberOfQuestions,_player1Buffer, _player2Buffer);
        string y = "";
        
        for (int i = 0; i < x.Length; i++)
        {
            if (i == 0)
            {
                y = x[i].ToString();
            }
            else
            {
                y = y + "," + x[i].ToString();
            }
        }

        _p1Buffer.text = _player1Buffer;
        _p2Buffer.text = _player2Buffer;
        _p1BufferCount.text = _player1Buffer.Split(',').Length.ToString();
        _p2BufferCount.text = _player2Buffer.Split(',').Length.ToString();
        _displayQuestions.text = y;
    }

    /* _Question Generator_
     * Description:
     * Method that generates a sorted array (in ascending) of int numbers excluding all numbers included in the playerpref buffers of Player1 and Player2
     * Parameters:
     * totalQuestionCount = Take From CSV
     * numberOfQuestions = Number of questions in a round
     * player1/2Buffers = Take value from playerpref of both players
     * --PlayerBuffer Format--
     * Type: String
     * Format: x,y,z,...
     * Example: 1,12,45,2,19
     */

    public int[] GenerateQuestions(int numberOfQuestions, string player1Buffer, string player2Buffer)
    {
        // Minimum and maximum question index values are being set in Start() of CQuizNDGenerator.cs (this script)
        
        int _lowerQuestionIndexVal;
        int _csvTotalQuestionCount;
        if (CQuizGameFSM.Instance.CurrentGameMode == CQuizGameFSM.EGameMode.Event ||
            CQuizGameFSM.Instance.CurrentGameMode == CQuizGameFSM.EGameMode.Main_Event)
        {
            _lowerQuestionIndexVal = ObscuredPrefs.GetInt("EventModeBufferIndexMin", 20001);
            _csvTotalQuestionCount = ObscuredPrefs.GetInt("EventModeBufferIndexMax", 20020) - ObscuredPrefs.GetInt("EventModeBufferIndexMin", 20001);
        }
        else
        {
            _lowerQuestionIndexVal = ObscuredPrefs.GetInt("OtherModesBufferIndexMin", 1);
            _csvTotalQuestionCount = ObscuredPrefs.GetInt("OtherModesBufferIndexMax", 169);
        }
        BufferCount = ((_csvTotalQuestionCount - numberOfQuestions) / 2) - 1;

        int[] _generatedQuestions = new int[numberOfQuestions];
        List<int> _exclusions;
        int[] _exclusionArray;  //Used to hold array values before list conversion
        
        if(player1Buffer == "" && player2Buffer == "")
        {
            _exclusions = new List<int>();
        }
        else if (player1Buffer == "")
        {
            _exclusionArray = Array.ConvertAll((player2Buffer).Split(',').Distinct().ToArray(), int.Parse);
            _exclusions = new List<int>(_exclusionArray);
        } else if (player2Buffer == "")
        {
            _exclusionArray = Array.ConvertAll((player1Buffer).Split(',').Distinct().ToArray(), int.Parse);
            _exclusions = new List<int>(_exclusionArray);
        } else
        {
            _exclusionArray = Array.ConvertAll((player2Buffer+","+player1Buffer).Split(',').Distinct().ToArray(), int.Parse);
            _exclusions = new List<int>(_exclusionArray);
        }
        _exclusions.Add(0);
        for (int i = 0; i < _generatedQuestions.Length; i++)
        {
            _generatedQuestions[i] = GenerateRandomAndExclude(_totalQuestionCount, _exclusions) + _lowerQuestionIndexVal;
            _exclusions.Add(_generatedQuestions[i] - _lowerQuestionIndexVal);
        }
        
        return _generatedQuestions;
    }
    #endregion

    #region PrivateMethods
    /*
     * _Random Number Generator_
     * Description:
     * Generates random numbers using an O(1n) solution in ascending order from 0 to maximum value excluding all integers inside the Exclusions array.
     * Parameters:
     * MaxValues = Maximum end of range of numbers to generate
     * Exclusions = Integer list of numbers to exclude from the generation
     */
    public static int GenerateRandomAndExclude(int maxValues, List<int> exclusions)
    {
        exclusions.Sort();
        Random r = new Random();
        if (maxValues - exclusions.Count <= 0)
        {
            exclusions.Clear();
            Debug.Log("ERROR! NO MORE POSSIBLE QUESTIONS");
            return GenerateRandomAndExclude(maxValues, exclusions);
        }
        int result = r.Next(maxValues - exclusions.Count);
        for (int i = 0; i < exclusions.Count; i++) 
        {
            if (result < exclusions[i])
            {
                return result;
            }
            result++;
        }
        return result;
    }
    #endregion
}
