using UnityEngine;
using System.Threading;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;
using System.Text;

public class PortfishManager : MonoBehaviour
{
    private Portfish.Engine engine;
    private Thread engineThread;
    private UnityPlug unityPlug;
    private bool engineRunning = false;

    public TMP_Text statusText;
    public TMP_InputField commandInput;
    public Button sendButton;
    public Button testButton;

    private string pendingBestMove = "";

    public Color whitePieceColor = Color.red;
    public Color blackPieceColor = Color.blue;

    void Start()
    {
        InitializeEngine();

        sendButton.onClick.AddListener(OnSendCommand);

        InitializeBoard();
        UpdateBoardUI();

        testButton.onClick.AddListener(() => RequestMove(currentFEN, 1000));
    }

    void Update()
    {
        // Process pending moves on the main thread
        if (!string.IsNullOrEmpty(pendingBestMove))
        {
            ApplyMove(pendingBestMove);
            pendingBestMove = ""; // Reset after processing
        }
    }

    public void OnSendCommand()
    {
        string command = commandInput.text.Trim();
        if (!string.IsNullOrEmpty(command))
        {
            SendCommand(command);
            statusText.text = $"Sent: {command}";
            commandInput.text = "";
        }
    }

    private void InitializeEngine()
    {
        try
        {
            Debug.Log("Initializing Portfish engine...");

            // Create and setup the UnityPlug for communication
            unityPlug = new UnityPlug();
            unityPlug.OnMessageReceived += HandleMessage;
            Portfish.Plug.Init(unityPlug);

            // Start the engine in a separate thread
            engineRunning = true;
            engineThread = new Thread(EngineThread);
            engineThread.IsBackground = true;
            engineThread.Start();

            // Wait a moment for the thread to start, then initialize UCI
            Invoke("InitializeUCI", 0.5f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing engine: {e.Message}");
        }
    }

    private void EngineThread()
    {
        try
        {
            Debug.Log("Engine thread started");
            engine = new Portfish.Engine();
            engine.Run(new string[0]); // Empty args to use the UCI loop
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Engine thread error: {e.Message}");
        }
    }

    private void InitializeUCI()
    {
        if (unityPlug != null)
        {
            Debug.Log("Sending isReady initialization commands");
            //unityPlug.SendCommand("uci");
            unityPlug.SendCommand("isready");
            //unityPlug.SendCommand("ucinewgame");
        }
    }

    string messageCollection = "";

    public void SendCommand(string command)
    {
        if (unityPlug != null && engineRunning)
        {
            unityPlug.SendCommand(command);
        }
    }

    void OnApplicationQuit()
    {
        engineRunning = false;

        if (unityPlug != null)
        {
            unityPlug.SendCommand("quit");
        }

        if (engineThread != null && engineThread.IsAlive)
        {
            // Give the thread a moment to exit gracefully
            if (!engineThread.Join(1000))
            {
                engineThread.Abort();
            }
        }

        Debug.Log("Portfish engine shutdown complete");
    }

    // You can call this from other parts of your code
    public void RequestMove(string fenPosition, int thinkTimeMs = 1000)
    {
        SendCommand($"position fen {fenPosition}");
        SendCommand($"go movetime {thinkTimeMs}");
        statusText.text = "Calculating move...";
    }

    private string currentFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private char[] board = new char[64];
    private void InitializeBoard()
    {
        // Parse FEN string to initialize the board array
        string[] fenParts = currentFEN.Split(' ');
        string[] ranks = fenParts[0].Split('/');

        int index = 0;
        foreach (string rank in ranks)
        {
            foreach (char c in rank)
            {
                if (char.IsDigit(c))
                {
                    int emptySquares = int.Parse(c.ToString());
                    for (int i = 0; i < emptySquares; i++)
                    {
                        board[index++] = ' ';
                    }
                }
                else
                {
                    board[index++] = c;
                }
            }
        }
    }

    public TMP_Text[] squareTexts;
    private void UpdateBoardUI()
    {
        try
        {
            Debug.Log("UBUI Start");
            for (int i = 0; i < 64; i++)
            {
                squareTexts[i].text = board[i] == ' ' ? "" : board[i].ToString();

                if (char.IsUpper(board[i]))
                {
                    squareTexts[i].color = whitePieceColor;
                }
                else
                {
                    squareTexts[i].color = blackPieceColor;
                }
                
            }
            Debug.Log("UBUI End");
        }
        catch (Exception e)
        {
            Debug.Log($"Exception {e.Message}");
        }
        
    }

    public void ApplyMove(string move)
    {
        Debug.Log("ApplyMove Enter");
        if (IsValidMove(move))
        {
            Debug.Log("Move is Valid");
            // Parse UCI move (e.g., "e2e4")
            int fromFile = move[0] - 'a';
            int fromRank = 8 - int.Parse(move[1].ToString());
            int toFile = move[2] - 'a';
            int toRank = 8 - int.Parse(move[3].ToString());

            int fromIndex = fromRank * 8 + fromFile;
            int toIndex = toRank * 8 + toFile;

            // Move the piece
            board[toIndex] = board[fromIndex];
            board[fromIndex] = ' ';

            // Handle promotion
            if (move.Length == 5)
            {
                board[toIndex] = move[4]; // Promotion piece
            }

            // Update the FEN string
            UpdateFENAfterMove(move);

            // Update the UI
            Debug.Log("Update Board");
            UpdateBoardUI();
            Debug.Log("End Update Board");

            Debug.Log($"Applied move: {move}");
            statusText.text = $"Applied move: {move}";
            Debug.Log($"New FEN: {currentFEN}");
        }
        else
        {
            Debug.LogWarning($"Invalid move: {move}");
            statusText.text = $"Invalid move: {move}";
        }
    }

    private void UpdateFENAfterMove(string move)
    {
        // For now, we'll use a simple approach
        // In a complete implementation, you would need to handle:
        // 1. Active color (switching between white and black)
        // 2. Castling availability
        // 3. En passant target square
        // 4. Halfmove clock
        // 5. Fullmove number
        
        // Simple approach: just update the board part of the FEN
        string[] fenParts = currentFEN.Split(' ');
        fenParts[0] = GenerateBoardFEN();
        
        // Toggle active color
        fenParts[1] = fenParts[1] == "w" ? "b" : "w";
        
        // For castling, en passant, and move counters, we'll keep them the same for now
        // In a real implementation, you would need to update these based on the move
        
        currentFEN = string.Join(" ", fenParts);
    }

    private string GenerateBoardFEN()
    {
        StringBuilder fen = new StringBuilder();
        
        for (int rank = 0; rank < 8; rank++)
        {
            int emptyCount = 0;
            
            for (int file = 0; file < 8; file++)
            {
                int index = rank * 8 + file;
                if (board[index] == ' ')
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        fen.Append(emptyCount);
                        emptyCount = 0;
                    }
                    fen.Append(board[index]);
                }
            }
            
            if (emptyCount > 0)
            {
                fen.Append(emptyCount);
            }
            
            if (rank < 7)
            {
                fen.Append("/");
            }
        }
        
        return fen.ToString();
    }

    // Check if a move is valid (basic validation)
    private bool IsValidMove(string move)
    {
        if (string.IsNullOrEmpty(move) || (move.Length != 4 && move.Length != 5))
            return false;

        // Check if the move follows UCI format (e.g., e2e4 or e7e8q)
        if (!char.IsLetter(move[0]) || !char.IsDigit(move[1]) ||
            !char.IsLetter(move[2]) || !char.IsDigit(move[3]))
            return false;

        // Check if the from square has a piece
        int fromFile = move[0] - 'a';
        int fromRank = 8 - int.Parse(move[1].ToString());
        int fromIndex = fromRank * 8 + fromFile;

        if (fromIndex < 0 || fromIndex >= 64 || board[fromIndex] == ' ')
            return false;

        return true;
    }

    // Update the HandleMessage method to apply the move when received
    private void HandleMessage(string message)
    {
        message = message.Trim();
        Debug.Log($"HandleMessage: [{message}]");

        messageCollection += $"{message} ";


        // This regex pattern will capture the move immediately after "bestmove"
        var match = Regex.Match(messageCollection, @"bestmove\s+(\S{4,5})(?:\s+|$)");

        if (match.Success)
        {
            string bestMove = match.Groups[1].Value;
            Debug.Log($"Parsed best move: {bestMove}");
            
            // Apply the move to the board
            pendingBestMove = bestMove;
            
            messageCollection = "";
        }

        
    }

    // Add a method to reset the board to the starting position
    public void ResetBoard()
    {
        currentFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        InitializeBoard();
        UpdateBoardUI();
        statusText.text = "Board reset to starting position";
    }
}