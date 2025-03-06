using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System;
using UnityEngine.UI;
using DG.Tweening.Plugins;

public class GameController : GameBaseController
{
    public static GameController Instance = null;
    public CharacterSet[] characterSets;
    public GridManager gridManager;
    public Cell[,] grid;
    public GameObject playerPrefab;
    public Transform parent;
    public Sprite[] defaultAnswerBox;
    public List<PlayerController> playerControllers = new List<PlayerController>();
    public bool showCells = false;
    public CanvasGroup[] audioTypeButtons, fillInBlankTypeButtons;
    public TextMeshProUGUI choiceText;
    public Transform[] flyingPositions;

    public string fullQAText = "";
    public string hiddenPart = "";
    public char[] correctAnswersLetter;
    public string[] partWords;
    public int fillLetterCount = 0;

    public CenterFillWords centerFillWords;
    public int nextCount = 1;

    protected override void Awake()
    {
        if (Instance == null) Instance = this;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        this.CreateGrids();
    }

    void CreateGrids()
    {
        Sprite gridTexture = LoaderConfig.Instance.gameSetup.gridTexture != null ?
                            SetUI.ConvertTextureToSprite(LoaderConfig.Instance.gameSetup.gridTexture as Texture2D) : null;
        this.grid = gridManager.CreateGrid(gridTexture);
    }

    private IEnumerator InitialQuestion()
    {
        this.createPlayer();

        var questionController = QuestionController.Instance;
        if(questionController == null) yield break;
        questionController.nextQuestion();
        this.controlDuplicateUIButtons(questionController);

        yield return new WaitForEndOfFrame();

        if (questionController.currentQuestion.answersChoics != null &&
            questionController.currentQuestion.answersChoics.Length > 0)
        {
            string[] answers = questionController.currentQuestion.answersChoics;
            this.gridManager.UpdateGridWithWord(answers, null);
        }
        else
        {
            string word = questionController.currentQuestion.qa.question;
            this.gridManager.UpdateGridWithWord(null, word);
            this.centerFillWords.UpdateFillWords(word);
        }
    }

    

    void createPlayer()
    {
        var cellPositions = this.gridManager.availablePositions;
        var characterPositionList = this.gridManager.CharacterStartupPositionsCellIds;

        for (int i = 0; i < this.maxPlayers; i++)
        {
            if (i < this.playerNumber)
            {
                var playerController = GameObject.Instantiate(this.playerPrefab, this.parent).GetComponent<PlayerController>();
                playerController.gameObject.name = "Player_" + i;
                playerController.UserId = i;
                this.playerControllers.Add(playerController);
                var cellVector2 = cellPositions[characterPositionList[i]];
                Vector3 actualCellPosition = this.gridManager.cells[cellVector2.x, cellVector2.y].transform.localPosition;
                this.gridManager.cells[cellVector2.x, cellVector2.y].setCellEnterColor(true);
                this.playerControllers[i].Init(this.characterSets[i], this.defaultAnswerBox, actualCellPosition);

                if (i == 0 && LoaderConfig.Instance != null && LoaderConfig.Instance.apiManager.peopleIcon != null)
                {
                    var _playerName = LoaderConfig.Instance?.apiManager.loginName;
                    var icon = SetUI.ConvertTextureToSprite(LoaderConfig.Instance.apiManager.peopleIcon as Texture2D);
                    this.playerControllers[i].UserName = _playerName;
                    this.playerControllers[i].updatePlayerIcon(true, _playerName, icon);
                }
                else
                {
                    var icon = SetUI.ConvertTextureToSprite(this.characterSets[i].defaultIcon as Texture2D);
                    this.playerControllers[i].updatePlayerIcon(true, null, icon);
                }
            }
            else
            {
                int notUsedId = i + 1;
                var notUsedPlayerIcon = GameObject.FindGameObjectWithTag("P" + notUsedId + "_Icon");
                if (notUsedPlayerIcon != null) { 
                    var notUsedIcon = notUsedPlayerIcon.GetComponent<PlayerIcon>();

                    if(notUsedIcon != null)
                    {
                        notUsedIcon.HiddenIcon();
                    }
                    //notUsedPlayerIcon.SetActive(false);
                }

                var notUsedPlayerController = GameObject.FindGameObjectWithTag("P" + notUsedId + "-controller");
                if (notUsedPlayerController != null) notUsedPlayerController.SetActive(false);
                /*if (notUsedPlayerController != null)
                {
                    var notUsedMoveController = notUsedPlayerController.GetComponent<CharacterMoveController>();
                    notUsedMoveController.TriggerActive(false);
                }*/
                // notUsedPlayerController.SetActive(false);
            }
        }
    }


    public override void enterGame()
    {
        base.enterGame();
        StartCoroutine(this.InitialQuestion());
    }

    public override void endGame()
    {
        bool showSuccess = false;
        for (int i = 0; i < this.playerControllers.Count; i++)
        {
            if(i < this.playerNumber)
            {
                var playerController = this.playerControllers[i];
                if (playerController != null)
                {
                    if (playerController.Score >= 30)
                    {
                        showSuccess = true;
                    }
                    this.endGamePage.updateFinalScore(i, playerController.Score);
                }
            }
        }
        this.endGamePage.setStatus(true, showSuccess);
        base.endGame();
    }

    void controlDuplicateUIButtons(QuestionController questionController = null)
    {
        var currentQuestion = questionController.currentQuestion;
        switch (currentQuestion.questiontype)
        {
            case QuestionType.Audio:
                SetUI.SetWholeGroupTo(this.audioTypeButtons, true);
                break;
            case QuestionType.FillInBlank:
                SetUI.SetWholeGroupTo(this.fillInBlankTypeButtons, true);
                break;
            case QuestionType.None:
            case QuestionType.Picture:
            case QuestionType.Text:
            case QuestionType.Arrange:
                SetUI.SetWholeGroupTo(this.audioTypeButtons, false);
                SetUI.SetWholeGroupTo(this.fillInBlankTypeButtons, false);
                break;
            default:
                SetUI.SetWholeGroupTo(this.audioTypeButtons, false);
                SetUI.SetWholeGroupTo(this.fillInBlankTypeButtons, false);
                break;
        }

        if (this.choiceText != null)
        {
            if(currentQuestion.answersChoics.Length > 0)
            {
                string formattedText = ""; // Initialize a string to hold the formatted answers

                for (int i = 0; i < currentQuestion.answersChoics.Length; i++)
                {
                    char label = (char)('A' + i); // Convert index to corresponding letter
                    formattedText += label + ": " + currentQuestion.answersChoics[i] + "\n"; // Append each formatted answer
                }
                this.choiceText.text = formattedText; // Set the combined text to the txt
            }
            else
            {
                switch (currentQuestion.questiontype)
                {
                    case QuestionType.Text:
                        this.fullQAText = currentQuestion.qa.question;
                        int answerLength = currentQuestion.correctAnswer.Length;
                        this.hiddenPart = new string('_', answerLength);
                        this.correctAnswersLetter = currentQuestion.correctAnswer.ToCharArray();
                        this.UpdateDisplayedQuestion();
                        break;
                    case QuestionType.Arrange:
                        this.fullQAText = currentQuestion.qa.question;
                        this.partWords = SetConvert.ShuffleStringToStringArray(this.fullQAText);
                        this.hiddenPart = "";
                        this.UpdateDisplayedQuestion();
                        break;
                }
            }
        }
    }

    public void updateSentence(Cell cell, Action completeAction = null)
    {
        if (this.partWords.Length > 0)
        {
            if (this.fillLetterCount < this.partWords.Length)
            {
                string letter = cell.content.text;
                string space = string.IsNullOrEmpty(this.hiddenPart) ? "" : " ";

                if (this.fillLetterCount < this.partWords.Length)
                {
                    for (int i = 0; i < this.playerNumber; i++)
                    {
                        if (this.playerControllers[i] != null)
                        {
                            this.playerControllers[i].correctAction(space + letter);
                        }
                    }

                    this.hiddenPart += (space + letter);
                    completeAction?.Invoke();
                    this.fillLetterCount += this.nextCount;                   
                }
            }
        }
    }

    public void updateQAFillInBlank(Cell cell, Action correctAction=null, Action inCorrectAction=null)
    {
        if(this.correctAnswersLetter.Length > 0) {

            if(this.fillLetterCount < this.correctAnswersLetter.Length)
            {
                string letter = cell.content.text;
                string c = letter.ToLower();

                if(this.fillLetterCount + 1 < this.correctAnswersLetter.Length)
                {
                    this.nextCount = string.IsNullOrWhiteSpace(this.correctAnswersLetter[this.fillLetterCount + 1].ToString().ToLower()) ? 2 : 1;
                }
                else
                {
                    this.nextCount = 1;
                }

                if (this.correctAnswersLetter[this.fillLetterCount].ToString().ToLower() == c)
                {
                    this.hiddenPart = this.hiddenPart.Remove(this.fillLetterCount, 1).Insert(this.fillLetterCount, c.ToString());
                    string addLetter = this.nextCount == 2? c + " " : c;
                    for (int i = 0; i < this.playerNumber; i++)
                    {
                        if (this.playerControllers[i] != null)
                        {
                            this.playerControllers[i].correctAction(addLetter);
                        }
                    }
                    correctAction?.Invoke();
                    this.fillLetterCount += this.nextCount;
                }
                else
                {
                    inCorrectAction?.Invoke();
                }
            }         
        }
    }

    public Transform FlyingPosition(int centerfillId)
    {
       int fillWordId = this.fillLetterCount;
       return this.centerFillWords.storedFillWords[centerfillId].fillWords[fillWordId].transform;
    }

    public void UpdateDisplayedQuestion(string content = "")
    {
        //int existingUnderscoreCount = this.fullQAText.Count(c => c == '_');

        //if (existingUnderscoreCount > 0) {
        if (!string.IsNullOrEmpty(content)) { 
            //this.choiceText.text = this.fullQAText.Replace(new string('_', existingUnderscoreCount), this.hiddenPart);
            this.choiceText.text = this.hiddenPart;
        }
        else
        {
            this.choiceText.text = "";
        }

        this.centerFillWords.FillWord(this.gridManager, content, this.correctAnswersLetter);

        if (this.fillLetterCount == this.partWords.Length)
        {
            for (int i = 0; i < this.playerNumber; i++)
            {
                if (this.playerControllers[i] != null)
                {
                    this.playerControllers[i].finishedAction();
                }
            }
        }
    }


    public void PrepareNextQuestion()
    {
        LogController.Instance?.debug("Prepare Next Question");
        for (int i = 0; i < this.playerNumber; i++)
        {
            if (this.playerControllers[i] != null)
            {
                this.playerControllers[i].characterStatus = CharacterStatus.nextQA;
            }
        }
    }

    public void UpdateNextQuestion()
    {
        this.playersResetPosition();

        LogController.Instance?.debug("Next Question");
        this.fillLetterCount = 0;
        this.centerFillWords.resetWord();
        var questionController = QuestionController.Instance;

        if (questionController != null) {
            questionController.nextQuestion();

            this.controlDuplicateUIButtons(questionController);

            if (questionController.currentQuestion.answersChoics != null &&
                questionController.currentQuestion.answersChoics.Length > 0)
            {
                string[] answers = questionController.currentQuestion.answersChoics;
                this.gridManager.UpdateGridWithWord(answers, null);
            }
            else
            {
                string word = questionController.currentQuestion.qa.question;
                this.gridManager.UpdateGridWithWord(null, word);
                this.centerFillWords.UpdateFillWords(word);
            }
        }       
    }

    void playersResetPosition()
    {
        var cellPositions = this.gridManager.availablePositions;
        var characterPositionList = this.gridManager.CharacterStartupPositionsCellIds;

        for (int i = 0; i < this.playerNumber; i++)
        {
            if (this.playerControllers[i] != null)
            {
                var cellVector2 = cellPositions[characterPositionList[i]];
                Vector3 actualCellPosition = this.gridManager.cells[cellVector2.x, cellVector2.y].transform.localPosition;
                this.gridManager.cells[cellVector2.x, cellVector2.y].setCellEnterColor(true);
                this.playerControllers[i].resetRetryTime();
                this.playerControllers[i].collectedCell.Clear();
                this.playerControllers[i].playerReset(actualCellPosition);
            }
        }
    }
   
    
    private void Update()
    {
        if(!this.playing) return;

        if(Input.GetKeyDown(KeyCode.F1))
        {
            this.showCells = !this.showCells;
            this.gridManager.setAllCellsStatus(this.showCells);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            this.playersResetPosition();
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            this.UpdateNextQuestion();
        }

        if (this.playerControllers.Count == 0) return;
        bool isNextQuestion = true;
        for (int i = 0; i < this.playerNumber; i++)
        {
            if (this.playerControllers[i] == null || !this.playerControllers[i].IsTriggerToNextQuestion)
            {
                isNextQuestion = false;
                break;
            }
        }

        if (isNextQuestion)
        {
            this.UpdateNextQuestion();
        }
    } 
}


public enum CharacterStatus
{
    born,
    idling,
    moving,
    nextQA
}

[Serializable]
public class CenterFillWords
{
    public GameObject fillWordPrefab;
    public GridLayoutGroup[] filledWords;
    public StoredFillWords[] storedFillWords;
    public int centerLetterCount = 0;
    public int defaultLetterLength = 13;
    public int defaultCellX = 94;
    public int defaultSpaceX = 15;
    public int padding_Top;
    public int padding_Bottom;
    public int padding_Left;
    public int padding_Right;
    public int nextCount = 1;

    public void UpdateFillWords(string word)
    {
        string[] wordParts = SetConvert.ShuffleStringToStringArray(word);
        int wordLength = wordParts.Length;
        int overLetterLength = wordLength - this.defaultLetterLength;
        int _cellX = overLetterLength > 0 ? (this.defaultCellX - (overLetterLength * 5)) : this.defaultCellX;
        int _spaceX = overLetterLength > 0 ? (this.defaultSpaceX - (overLetterLength * 2)) : this.defaultSpaceX;

        for (int i = 0; i < this.filledWords.Length; i++)
        {
            this.filledWords[i].cellSize = new Vector2Int(_cellX, _cellX);
            this.filledWords[i].spacing = new Vector2Int(_spaceX, 0);
            this.filledWords[i].padding.top = this.padding_Top;
            this.filledWords[i].padding.bottom = this.padding_Bottom;
            this.filledWords[i].padding.left = this.padding_Left;
            this.filledWords[i].padding.right = this.padding_Right;
            int storedFillWordsLength = this.storedFillWords[i].WordLength;
            int updateFillWords = wordLength - storedFillWordsLength;

            if (updateFillWords > 0)
            {
                for (int j = 0; j < wordLength; j++)
                {
                    if (j < this.storedFillWords[i].WordLength)
                    {
                        if (this.storedFillWords[i].WordLength > 0)
                        {
                            this.storedFillWords[i].fillWords[j].gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        var fillWord = GameObject.Instantiate(this.fillWordPrefab, this.filledWords[i].transform).GetComponent<FillWord>();
                        fillWord.init("FillWord_" + this.storedFillWords[i].WordLength);
                        this.storedFillWords[i].fillWords.Add(fillWord);
                    }

                    this.storedFillWords[i].fillWords[j].SetWord(string.IsNullOrWhiteSpace(wordParts[j]) ? false : true);
                }
            }
            else if (updateFillWords < 0)
            {
                for (int j = 0; j < storedFillWordsLength; j++)
                {
                    if (j < wordLength)
                    {
                        this.storedFillWords[i].fillWords[j].gameObject.SetActive(true);
                        this.storedFillWords[i].fillWords[j].SetWord(string.IsNullOrWhiteSpace(wordParts[j]) ? false : true);
                    }
                    else
                    {
                        this.storedFillWords[i].fillWords[j].gameObject.SetActive(false);
                    }
                }
            }
            else if (updateFillWords == 0)
            {
                for (int j = 0; j < storedFillWordsLength; j++)
                {
                    this.storedFillWords[i].fillWords[j].gameObject.SetActive(true);
                    this.storedFillWords[i].fillWords[j].SetWord(string.IsNullOrWhiteSpace(wordParts[j]) ? false : true);
                }
            }

            this.storedFillWords[i].ResetWords();
        }
    }

    public void FillWord(GridManager gridManager=null, string content = "", char[] correctAnswersLetter = null)
    {
        if (!string.IsNullOrEmpty(content))
        {
            string c = content;
            if (this.centerLetterCount + 1 < correctAnswersLetter.Length)
            {
                this.nextCount = string.IsNullOrWhiteSpace(correctAnswersLetter[this.centerLetterCount + 1].ToString().ToLower()) ? 2: 1;
            }
            else
            {
                this.nextCount = 1;
            }

            for (int i = 0; i < this.storedFillWords.Length; i++)
            {
                this.storedFillWords[i].fillWords[this.centerLetterCount].SetContent(c);
            }
            this.centerLetterCount += this.nextCount;

            for (int i = 0; i < this.storedFillWords.Length; i++)
            {
                this.storedFillWords[i].SetNextLetterHint(this.centerLetterCount);
            }
        }
    }

    public void resetWord()
    {
        this.centerLetterCount = 0;
    }
}


[Serializable]
public class StoredFillWords
{
    public string name;
    public List<FillWord> fillWords = new List<FillWord>();

    public int WordLength
    {
        get
        {
            return this.fillWords.Count;
        }
    }

    public void SetNextLetterHint(int nextId)
    {
        for (int i = 0; i < this.fillWords.Count; i++)
        {
            if (i == nextId)
                this.fillWords[i].SetHint(true);
            else
                this.fillWords[i].SetHint(false);
        }
    }

    public void ResetWords()
    {
        for(int i = 0; i < this.fillWords.Count; i++)
        {
            if(i == 0) 
                this.fillWords[i].SetHint(true);
            else
                this.fillWords[i].SetHint(false);

            if (this.fillWords[i] != null) {
                this.fillWords[i].SetContent("");
            }
        }
    }
}