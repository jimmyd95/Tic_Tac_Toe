using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum TicTacToeState{none, cross, circle, draw} // I created a new Enum so it can detect a draw

[System.Serializable]
public class WinnerEvent : UnityEvent<int>
{
}

public class TicTacToeAI : MonoBehaviour
{

	int _aiLevel;

	TicTacToeState[,] boardState;

	[SerializeField]
	private bool _isPlayerTurn;

	[SerializeField]
	private int _gridSize;
	
	[SerializeField]
	private TicTacToeState playerState = TicTacToeState.cross;
	TicTacToeState aiState = TicTacToeState.circle;

	[SerializeField]
	private GameObject _xPrefab;

	[SerializeField]
	private GameObject _oPrefab;

	public UnityEvent onGameStarted;

	//Call This event with the player number to denote the winner
	public WinnerEvent onPlayerWin;

	ClickTrigger[,] _triggers;

	private void Awake()
	{
		_gridSize = 3;
		if(onPlayerWin == null){
			onPlayerWin = new WinnerEvent();
		}
		boardState = new TicTacToeState[_gridSize, _gridSize];
	}

    private void Update() {

		TicTacToeState winner = checkForWinner(boardState);
		if (winner == TicTacToeState.none)
        {
			if (_aiLevel == 0 && !_isPlayerTurn)
			{
				var temp = randomChoice(); // randomly pick one that is not empty
										   // I have yet to figure out how to pick the bloody Easy and Hard selection...
				AiSelects(temp.x, temp.y);
			}
			else if (_aiLevel == 1 && !_isPlayerTurn)
			{
				// Yes I made the minmax algorithm (by not reading the instruction from Eligibility Test...
				// yeah should read it more carefully)
				MinMaxAI();
			}
        }
        else
        {
			int winnerNum = (winner == TicTacToeState.draw) ? -1 : (winner == playerState) ? 0 : 1;
			onPlayerWin.Invoke(winnerNum);
		}
    }

	public void StartAI(int AILevel){
		_aiLevel = AILevel;
		// It causes NullReferenceException before starting the game and after creating the object
		// Not sure how to remove that......
		_isPlayerTurn = (_aiLevel == 0) ? true : false;
		StartGame();
    }

    public void RegisterTransform(int myCoordX, int myCoordY, ClickTrigger clickTrigger)
	{
		_triggers[myCoordX, myCoordY] = clickTrigger;
	}

	private void StartGame()
	{
		_triggers = new ClickTrigger[_gridSize, _gridSize];
        onGameStarted.Invoke();
    }

	public void PlayerSelects(int coordX, int coordY){
		_isPlayerTurn = false;
		SetVisual(coordX, coordY, playerState);
        boardState[coordX, coordY] = playerState;
		_triggers[coordX, coordY].GetComponent<ClickTrigger>().SetInputEndabled(false);
        //Debug.Log($"Player: {boardState[coordX, coordY]}");
    }

	public void AiSelects(int coordX, int coordY){
		
		SetVisual(coordX, coordY, aiState);
        boardState[coordX, coordY] = aiState; // register the state in board
		// set so you can't click on that tile anymore
		_triggers[coordX, coordY].GetComponent<ClickTrigger>().SetInputEndabled(false);

		TicTacToeState winner = checkForWinner(boardState);
        _isPlayerTurn = true;
		//Debug.Log($"AI: {boardState[coordX, coordY]}");
	}

	// This should be the "easy" option
	(int x, int y) randomChoice()
    {
		int x, y;
        do
        {
			x = UnityEngine.Random.Range(0, _gridSize);
			y = UnityEngine.Random.Range(0, _gridSize);
			//Debug.Log("current value of the coordinates: " + " "+ x + " "+ y);
		} while (boardState[x, y] != TicTacToeState.none);

        return (x, y);
    }

	// check every reasonable direction if it reached win state
	private TicTacToeState checkForWinner(TicTacToeState[,] state)
	{
		// check the bloody horizontal and vertical
		// I don't ever want to do this again......
		for (int i = 0; i < _gridSize; i++)
		{
			if (state[i, 0] != TicTacToeState.none && state[i, 0] == state[i, 1] && state[i, 1] == state[i, 2])
			{
				//Debug.Log("Horizontal: " + state[i,0] + " has been triggered");
				return state[i, 0];
			}
			if (state[0, i] != TicTacToeState.none && state[0, i] == state[1, i] && state[1, i] == state[2, i])
			{
				//Debug.Log("Vertical: " + state[0, i] + " has been triggered");
				return state[0, i];
			}
		}


		// Assign diag1 and diag2 to check if any of the diagonals connected
		if (state[0, 0] != TicTacToeState.none && state[0, 0] == state[1, 1] && state[1, 1] == state[2, 2])
		{
			//Debug.Log("Right top diagno: " + state[0, 0] + " has been triggered");
			return state[0, 0];
		}
		if (state[0, 2] != TicTacToeState.none && state[0, 2] == state[1, 1] && state[1, 1] == state[2, 0])
		{
			//Debug.Log("Left bottom Diagno: " + state[0, 2] + " has been triggered");
			return state[0, 2];
		}

		// Check for tie
		bool isTie = true;
		for (int i = 0; i < _gridSize; i++)
		{
			for (int j = 0; j < _gridSize; j++)
			{
				if (state[i, j] == TicTacToeState.none)
				{
					isTie = false;
					break;
				}
			}
			if (!isTie)
			{
				break;
			}
		}
		if (isTie)
		{
			return TicTacToeState.draw;
		}

		return TicTacToeState.none;
	}

	private void SetVisual(int coordX, int coordY, TicTacToeState targetState)
	{
		Instantiate(
			targetState == TicTacToeState.circle ? _oPrefab : _xPrefab,
			_triggers[coordX, coordY].transform.position,
			Quaternion.identity
		);
		//Debug.Log("Someone selected: " + coordX + ", " + coordY + " " + targetState);
	}




	// The MinMax algorithm. essentially it checks grid to see there is a local maximum score
	// if the score is higher than the current overall score, change the overall to local score and implement the move
	private void MinMaxAI()
	{
		int bestScore = int.MinValue;
		int[] bestMove = new int[] { -1, -1 };
		for (int i = 0; i < _gridSize; i++)
		{
			for (int j = 0; j < _gridSize; j++)
			{
				if (boardState[i, j] == TicTacToeState.none)
				{
					boardState[i, j] = aiState;
					int score = miniMax(boardState, _aiLevel, false);
					boardState[i, j] = TicTacToeState.none;

					if (score > bestScore)
					{
						bestScore = score;
						bestMove = new int[] { i, j };
					}
				}
			}
		}

		AiSelects(bestMove[0], bestMove[1]);
	}

	// this is for establishing the local max
	private int miniMax(TicTacToeState[,] state, int weight, bool check)
	{

		TicTacToeState winner = checkForWinner(state);
		// check if there's a winner, then test the weight of the game,
		// the number that gets subtracted or subtracting doesn't matter
		// lower the weight, higher the score priority (the AI wants to win more)
		if (winner != TicTacToeState.none)
		{
			if (winner == aiState)
			{
				return 1 - weight;
			}
			else
			{
				return weight - 1;
			}
		}

		// if the weight is zero, it means the game is still on ;DDDDD
		if (weight == 0)
		{
			return weight;
		}

		int bestScore = check ? int.MinValue : int.MaxValue;

		// I hate grids... So you run through every single item within the grid
		// and check if current i,j state matches the tictactocstate.none. if so,
		// clone a old state to a new state, use the new state to check if there's a score between the player and AI
		// if check 
		for (int i = 0; i < _gridSize; i++)
		{
			for (int j = 0; j < _gridSize; j++)
			{
				if (state[i, j] == TicTacToeState.none)
				{
					TicTacToeState[,] newState = (TicTacToeState[,])state.Clone();
					newState[i, j] = check ? aiState : playerState;
					int score = miniMax(newState, weight - 1, !check);

					if (check)
					{
						bestScore = Mathf.Max(bestScore, score);
					}
					else
					{
						bestScore = Mathf.Min(bestScore, score);
					}
				}
			}
		}

		return bestScore;
	}

}
