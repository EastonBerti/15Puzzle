//Easton Berti
//eberti@nevada.unr.edu
using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using static System.Threading.Timer;
using System.Timers;
using TMPro;
/*public int[,] current = new int[3, 3]
{
{ 0, 1, 2},
{ 3, 4, 5},
{ 6, 7, 8}
};
public int[,] finalCompletion = new int[3, 3]
{
{ 0, 1, 2},
{ 3, 4, 5},
{ 6, 7, 8}
};*/
public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform gameTransform;
    [SerializeField] private Transform piecePrefab;
    [SerializeField] private TMP_Text moveHint;
    [SerializeField] private TMP_Text completionCount;
    public Button hintButton, shuffleButton;
    public List<Transform> pieces;
    private int emptyLocation;
    private int size;
    private bool shuffling = false;
    public AudioSource audio;
    public int completedPuzzles = 0;
    public AudioSource backgroundMusic;
    public AudioSource popSFX;
    public int[,] current = new int[4, 4]
    {
        { 0, 1, 2, 3 },
        { 4, 5, 6, 7 },
        { 8, 9, 10, 11 },
        { 12, 13, 14, 15 }
    };
    public int[,] finalCompletion = new int[4, 4]
    {
        { 0, 1, 2, 3 },
        { 4, 5, 6, 7 },
        { 8, 9, 10, 11 },
        { 12, 13, 14, 0}
   };

    //Create game setup with size x size pieces
    private void CreateGamePieces(float gapThickness)
    {
        float width = 1 / (float)size;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                Transform piece = Instantiate(piecePrefab, gameTransform);
                pieces.Add(piece);
                piece.localPosition = new Vector3(-1 + (2 * width * col) + width, +1 - (2 * width * row) - width, 0);
                piece.localScale = ((2 * width) - gapThickness) * Vector3.one;
                piece.name = $"{(row * size) + col}";

                if ((row == size - 1) && (col == size - 1))
                {
                    emptyLocation = (size * size) - 1;
                    piece.gameObject.SetActive(false);
                }
                else
                {
                    float gap = gapThickness / 2;
                    Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                    Vector2[] uv = new Vector2[4];
                    uv[0] = new Vector2((width * col) + gap, 1 - ((width * (row + 1)) - gap));
                    uv[1] = new Vector2((width * (col + 1)) - gap, 1 - ((width * (row + 1)) - gap));
                    uv[2] = new Vector2((width * col) + gap, 1 - ((width * row) + gap));
                    uv[3] = new Vector2((width * (col + 1)) - gap, 1 - ((width * row) + gap));
                    mesh.uv = uv;
                }
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        pieces = new List<Transform>();
        
        size = 4;
        CreateGamePieces(0.01f);
        shuffling = true;
        StartCoroutine(WaitShuffle(0.5f));
        backgroundMusic.Play();

        hintButton.onClick.AddListener(delegate { Solve(); });
        shuffleButton.onClick.AddListener(delegate { StartCoroutine(WaitShuffle(0.5f)); });
    }

    // Update is called once per frame
    void Update()
    {

        //check for completion
        if (!shuffling && CheckCompletion())
        {
            completedPuzzles++;
            completionCount.text = ("Completed Puzzles: " + completedPuzzles);
            moveHint.text = ("");
            shuffling = true;
            StartCoroutine(WaitShuffle(0.5f));
            audio.Play();
        }

        //on click send out ray to see if we click a piece
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (pieces[i] == hit.transform)
                    {
                        //check each direction to see if move is valid, break out on success so we dont carry on and swap again
                        if (SwapIfValid(i, -size, size)) { break; }
                        if (SwapIfValid(i, +size, size)) { break; }
                        if (SwapIfValid(i, -1, 0)) { break; }
                        if (SwapIfValid(i, +1, size - 1)) { break; }


                    }
                }
            }
        }
    }
    //colCheck used to stop horizontal moves wrapping
    private bool SwapIfValid(int i, int offset, int colCheck)
    {
        if (((i % size) != colCheck) && ((i + offset) == emptyLocation))
        {
            //swap in game state
            (pieces[i], pieces[i + offset]) = (pieces[i + offset], pieces[i]);
            //swap their transforms
            (pieces[i].localPosition, pieces[i + offset].localPosition) = ((pieces[i + offset].localPosition, pieces[i].localPosition));
            //update empty location
            emptyLocation = i;
            popSFX.Play();
            return true;
        }
        return false;
    }
    private bool CheckCompletion()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].name != $"{i}")
            {
                return false;
            }
        }
        return true;
    }
    private IEnumerator WaitShuffle(float duration)
    {
        yield return new WaitForSeconds(duration);
        Shuffle();
        shuffling = false;
    }
    private void Shuffle()
    {
        int count = 0;
        int last = 0;
        while (count < (size + size ))
        {
            //pick rand location
            int rnd = Random.Range(0, (size * size));
            //only thing forbid is undoing last move
            if (rnd == last) { continue; }
            last = emptyLocation;
            //try surrounding spaces for valid moce
            if (SwapIfValid(rnd, -size, size))
            {
                count++;
            }
            else if (SwapIfValid(rnd, +size, size))
            {
                count++;
            }
            else if (SwapIfValid(rnd, -1, 0))
            {
                count++;
            }
            else if (SwapIfValid(rnd, +1, size - 1))
            {
                count++;
            }
        }
    }

    private void convertBoardToMatrix(int[,] matrix, List<Transform> list)
    {
        int c = 0;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                matrix[i, j] = Convert.ToInt32(list[c].name);
                c++;
            }
        }
    }

    
    //Code assisted from ChatGPT line 218-368
    private class Node
    {
        public int[,] puzzle;
        public int x, y;
        public Node parent;

        public Node(int[,] puzzle, int x, int y, Node parent)
        {
            this.puzzle = puzzle;
            this.x = x;
            this.y = y;
            this.parent = parent;
        }
    }

    public void Solve()
    {
        
        updateCurrent();
        int x = 0, y = 0;
        // Find the position of the empty tile (0)
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (current[i, j] == 15)
                {
                    x = i;
                    y = j;
                }
            }
        }
        
        // Initialize the queue and add the current state to it
        Queue<Node> queue = new Queue<Node>();
        Node start = new Node(current, x, y, null);
        queue.Enqueue(start);

        // Initialize the HashSet
        HashSet<string> explored = new HashSet<string>();
        int q = 0;
        while (queue.Count > 0){
            Node currentQ = queue.Dequeue();

            // Check if we've reached the goal state
            if (checkSolution(currentQ.puzzle))
            {
                
                    // If we have found solution, print the next miove
                    PrintNext(currentQ);
                    return;
             
            }
            // Generate a string representation of the current state
            string state = MatrixToString(currentQ.puzzle);

            // Check if the current state has already been explored
            if (explored.Contains(state))
            {
                continue;
            }
            
            // Add the current state to the explored set
            explored.Add(state);

            // Generate the children of the current node and add them to the queue
            List<Node> children = GenerateChildren(currentQ);
            foreach (Node child in children)
            {
                queue.Enqueue(child);
                
            }
            q++;
        }
    }

        private List<Node> GenerateChildren(Node node)
    {
        List<Node> children = new List<Node>();

        int x = node.x;
        int y = node.y;

        // Move the empty tile up
        if (x > 0)
        {
            int[,] puzzle = CopyPuzzle(node.puzzle);
            puzzle[x, y] = puzzle[x - 1, y];
            puzzle[x - 1, y] = 0;
            children.Add(new Node(puzzle, x - 1, y, node));
        }

        // Move the empty tile down
        if (x < (size-1))
        {
            int[,] puzzle = CopyPuzzle(node.puzzle);
            puzzle[x, y] = puzzle[x + 1, y];
            puzzle[x + 1, y] = 0;
            children.Add(new Node(puzzle, x + 1, y, node));
        }

        // Move the empty tile left
        if (y > 0)
        {
            int[,] puzzle = CopyPuzzle(node.puzzle);
            puzzle[x, y] = puzzle[x, y - 1];
            puzzle[x, y - 1] = 0;
            children.Add(new Node(puzzle, x, y - 1, node));
        }

        // Move the empty tile right
        if (y < (size - 1))
        {
            int[,] puzzle = CopyPuzzle(node.puzzle);
            puzzle[x, y] = puzzle[x, y + 1];
            puzzle[x, y + 1] = 0;
            children.Add(new Node(puzzle, x, y + 1, node));
        }

        return children;
    }

    private int[,] CopyPuzzle(int[,] puzzle)
    {
        int[,] copy = new int[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                copy[i, j] = puzzle[i, j];
            }
        }
        return copy;
    }


    private void PrintNext(Node node)
    {
        List<Node> solution = new List<Node>();
        while (node != null)
        {
            solution.Add(node);
            node = node.parent;
        }

        solution.Reverse();
        //string solutionStep = MatrixToString(solution[0].puzzle);
       // string finalCompString = MatrixToString(finalCompletion);
       string direction = returnDirection(solution[0].puzzle,solution[1].puzzle);
       moveHint.text  =("Next move: " + direction);
    }



    // no longger made with help of chat GPT
    private string MatrixToString(int[,] matrix)
    {
        string s = "";
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                s += matrix[i, j] + " ";
            }
            s += "\n";
        }
        return s;
    }
    private void updateCurrent()
    {
        int c = 0;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                current[i, j] = Convert.ToInt32(pieces[c].name);
                c++;
                
            }
        }
    }
    private bool checkSolution(int[,] curSolution)
    {

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (curSolution[i, j] != finalCompletion[i, j])
                {
                    return false;
                }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               
            }
        }
        return true;
    }

    private string returnDirection(int[,] curSolution, int[,] nexSolution)
    {
        int a = 0;
        int b = 0;
        int x = 0;
        int y = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (curSolution[i, j] == 15)
                {
                    x = i; y = j;
                }
                if (nexSolution[i, j] == 0)
                {
                    a = i; b = j;
                }
            }
        }

        int tempXmi = x - 1;
        int tempXad = x + 1;
        int tempYmi = y - 1;
        int tempYad = y + 1;
        

        if (tempXmi == a &&  y == b)
        {

            return "up";
        }
        else if (x == a && tempYad == b)
        {

            return "right";
        }
        else if (tempXad == a && y == b)
        {

            return "down";
        }
        else if (x == a && tempYmi == b)
        {

            return "left";
        }
        return "error calculating";
    }

}
