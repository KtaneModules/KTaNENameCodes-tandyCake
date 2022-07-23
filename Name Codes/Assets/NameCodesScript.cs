using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class NameCodesScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMRuleSeedable Ruleseed;
    public KMSelectable leftArrow;
    public KMSelectable rightArrow;
    public KMSelectable submitButton;
    public TextMesh displayText;

    readonly string[] allWords = { "ANGLE", "GRAVE", "BARK", "RULER", "KITE", "WHILE", "QUERY", "ARROW", "TEEPEE", "PARTY", "DIJON", "TRAVEL", "BONE", "GREEN", "SPARKS", "SPINS", "VICE", "NOMEN", "VERTIGO", "PRESET", "CRYPT", "ROYAL", "INDEX", "JEWELS", "BEES", "BARN", "FOXES", "WEASEL", "QUEASY", "PENCIL", "WINTER", "JAIL", "YOLK", "QUOTA", "BANJO", "COAX", "JAZZ", "FILES", "CAGED", "PISTOL", "CHAMP", "GLOBE", "BASKETS", "MORGUE", "GELATIN", "QUALITY", "ARRAY", "LEAN", "WEARY", "ZEALOT", "JUNGLES", "FENCE", "KINDLY", "SUNNY", "SPECIES", "AGILE", "CRAM", "VISIT", "PSYCHIC", "VIAL", "ROUTE", "FINAL", "BAGEL" };
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    int leftIndex;
    int rightIndex;
    int stringLength;
    int currentPosition;
    char currentLetter;
    string[] usedWords;
    string chosenString;
    int solution;

    void Awake()
    {   
        moduleId = moduleIdCounter++;
        leftArrow.OnInteract += delegate () { LeftPress(); return false; };
        rightArrow.OnInteract += delegate () { RightPress(); return false; };
        submitButton.OnInteract += delegate () { SubmitPress(); return false; };
    }

    void Start()
    {
        SetUpRuleseed();
        GenerateString();
        GetPosition();
        GenerateIndices();
        solution = (2 * leftIndex + rightIndex) % 10;
        Debug.LogFormat("[Name Codes #{0}] Your solution digit is {1}.", moduleId, solution);
        displayText.text = currentLetter.ToString();

    }
    void SetUpRuleseed()
    {
        var rng = Ruleseed.GetRNG();
        if (rng.Seed != 1)
        {
            bool invalidSetup;
            do
            {
                rng.ShuffleFisherYates(allWords);
                var lengths = new Dictionary<int, int> { { 4, 0 }, { 5, 0 }, { 6, 0 }, { 7, 0 } };
                for (int i = 0; i < 21; i++)
                    lengths[allWords[i].Length]++;
                invalidSetup = lengths[7] > 2 ||
                                lengths[6] < 3 || lengths[6] > 5 ||
                                lengths[5] < 10 || lengths[5] > 15 ||
                                lengths[4] < 3 || lengths[4] > 5;
            } while (invalidSetup);
        }
        usedWords = allWords.Take(21).ToArray();
        Debug.Log(usedWords.Join());
    }

    void GenerateIndices()
    {
        do
        {
            leftIndex = UnityEngine.Random.Range(2, 6);
            rightIndex = UnityEngine.Random.Range(2, 6);
        } while (((double)leftIndex / rightIndex % 1 == 0) || ((double)rightIndex / leftIndex % 1 == 0));
        Debug.LogFormat("[Name Codes #{0}] Your left and right indices are {1} and {2}.", moduleId, leftIndex, rightIndex);
    }

    void GenerateString()
    {
        chosenString = usedWords.Shuffle().Take(5).Join("");
        stringLength = chosenString.Length;
        Debug.LogFormat("[Name Codes #{0}] Your chosen string is {1}.", moduleId, chosenString);
    }
    void GetPosition()
    {
        currentPosition = UnityEngine.Random.Range(0, stringLength);
        currentLetter = chosenString[currentPosition];
        Debug.LogFormat("[Name Codes #{0}] You are starting at position {1}, which is letter {2}.", moduleId, currentPosition + 1, currentLetter);
    }

    void LeftPress()
    {
        currentPosition = (currentPosition - leftIndex + stringLength) % stringLength;
        ArrowPress(leftArrow);
    }
    void RightPress()
    {
        currentPosition = (currentPosition + rightIndex) % stringLength;
        ArrowPress(rightArrow);
    }
    void ArrowPress(KMSelectable btn)
    {
        btn.AddInteractionPunch(0.2f);
        Audio.PlaySoundAtTransform("flip", btn.transform);
        currentLetter = chosenString[currentPosition];
        if (!moduleSolved)
            displayText.text = currentLetter.ToString();
    }
    void SubmitPress()
    {
        if (moduleSolved)
            return;
        submitButton.AddInteractionPunch(0.75f);
        int digitOnSubmit = ((int)Bomb.GetTime()) % 10;
        if (digitOnSubmit == solution)
        {
            moduleSolved = true;
            Audio.PlaySoundAtTransform("solve", transform);
            if (UnityEngine.Random.Range(0, 100) == 0) { displayText.text = "You are\nJon enough."; displayText.characterSize = 0.9f; displayText.color = Color.red; }
            else displayText.text = "!!";   
            Debug.LogFormat("[Name Codes #{0}] You submitted when the last digit of the countdown timer was {1}. Module solved.", moduleId, digitOnSubmit);
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        {
            Debug.LogFormat("[Name Codes #{0}] You submitted when the last digit of the countdown timer was {1}. That was incorrect.", moduleId, digitOnSubmit);
            GetComponent<KMBombModule>().HandleStrike();
        }
    }

    KMSelectable WhichButton(string direction)
    {
        if (direction == "LEFT" || direction == "L") 
            return leftArrow;
        else return rightArrow;
    }


#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} left 4 to press the left arrow 4 times. Directions can be abbreviated. Forgo the number to press the button once. Movements can be chained with spaces. Use !{0} submit 5 to press the letter when the last timer digit is 5.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        string[] possibleDirections = { "LEFT", "RIGHT", "L", "R" };
        string Command = input.Trim().ToUpperInvariant();
        List<string> parameters = Command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parameters.First() == "SUBMIT" && parameters.Count == 2 && parameters.Last().Length == 1 && "0123456789".Contains(parameters.Last()[0]))
        {
            yield return null;
            int submitting = int.Parse(parameters.Last());
            while ((int)Bomb.GetTime() % 10 == submitting)
                yield return "trycancel"; //Fixes the obscure TP bug which got Square Button on ON
            while ((int)Bomb.GetTime() % 10 != submitting)
                yield return "trycancel";
            submitButton.OnInteract();
        }
        else if (parameters.Count == 2 && possibleDirections.Contains(parameters[0]) && parameters[1].All(x => "1234567890".Contains(x)))
        {
            int value;
            if (int.TryParse(parameters[1], out value))
            {
                yield return null;
                for (int i = 0; i < value; i++)
                {
                    WhichButton(parameters.First()).OnInteract();
                    yield return "trycancel";
                    yield return new WaitForSeconds(0.75f);
                }
            }
        }
        else if (parameters.All(x => possibleDirections.Contains(x)))
        {
            yield return null;
            foreach (string command in parameters)
            {
                WhichButton(command).OnInteract();
                yield return "trycancel";
                yield return new WaitForSeconds(0.75f);
            }
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        while ((int)Bomb.GetTime() % 10 != solution)
            yield return true;
        submitButton.OnInteract();
        yield return new WaitForSeconds(0.05f);
    }
}
