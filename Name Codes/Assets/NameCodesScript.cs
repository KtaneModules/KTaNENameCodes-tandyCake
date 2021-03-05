using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class NameCodesScript : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable leftArrow;
    public KMSelectable rightArrow;
    public KMSelectable submitButton;
    public TextMesh displayText;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    int leftIndex;
    int rightIndex;
    int stringLength;
    int currentPosition;
    char currentLetter;
    string[] words = new string[] { "ANGLE", "GRAVE", "BARK", "RULER", "KITE", "WHILE", "QUERY", "ARROW", "TEEPEE", "PARTY", "DIJON", "TRAVEL", "BONE", "GREEN", "SPARKS", "SPINS", "VICE", "NOMEN", "VERTIGO", "PRESET", "CRYPT" };
    string chosenString;
    int solution;
    string[] parameters;


    void Awake()
    {
        moduleId = moduleIdCounter++;
        leftArrow.OnInteract += delegate () { LeftPress(); return false; };
        rightArrow.OnInteract += delegate () { RightPress(); return false; };
        submitButton.OnInteract += delegate () { SubmitPress(); return false; };
    }

    void Start()
    {
        GenerateString();
        GetPosition();
        GenerateIndices();
        solution = (2 * leftIndex + rightIndex) % 10;
        Debug.LogFormat("[Name Codes #{0}] Your solution digit is {1}.", moduleId, solution);

        displayText.text = currentLetter.ToString();

    }

    void GenerateIndices()
    {
        leftIndex = UnityEngine.Random.Range(2, 6);
        rightIndex = UnityEngine.Random.Range(2, 6);

        if ((Convert.ToDouble(leftIndex) / Convert.ToDouble(rightIndex) % 1 == 0) || (Convert.ToDouble(rightIndex) / Convert.ToDouble(leftIndex) % 1 == 0)) //Checks if the indices are equal or have factors.
        {
            GenerateIndices();
        }
        else Debug.LogFormat("[Name Codes #{0}] Your left and right indices are {1} and {2}.", moduleId, leftIndex, rightIndex);
    }

    void GenerateString()
    {
        words.Shuffle();
        chosenString = words.Take(5).Join("");
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
        if (moduleSolved)
        {
            return;
        }
        Audio.PlaySoundAtTransform("flip", transform);
        leftArrow.AddInteractionPunch(0.2f);
        currentPosition = (currentPosition - leftIndex + stringLength) % stringLength;
        currentLetter = chosenString[currentPosition];
        displayText.text = currentLetter.ToString();
    }
    void RightPress()
    {
        if (moduleSolved)
        {
            return;
        }
        Audio.PlaySoundAtTransform("flip", transform);
        rightArrow.AddInteractionPunch(0.2f);
        currentPosition = (currentPosition + rightIndex) % stringLength;
        currentLetter = chosenString[currentPosition];
        displayText.text = currentLetter.ToString();
    }
    void SubmitPress()
    {
        if (moduleSolved)
        {
            return;
        }
        submitButton.AddInteractionPunch(0.75f);
        int digitOnSubmit = Mathf.FloorToInt(Bomb.GetTime()) % 10;
        if (digitOnSubmit == solution)
        {

            Audio.PlaySoundAtTransform("solve", transform);
            if (UnityEngine.Random.Range(0, 100) == 0) { displayText.text = "You are\nJon enough."; displayText.characterSize = 1; displayText.color = new Color(1, 0, 0, 1); }
            else { displayText.text = "!!"; }   
            moduleSolved = true;
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
        if (direction == "LEFT" || direction == "L") { return leftArrow; }
        else return rightArrow;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} left 4 to press the left arrow 4 times. Use !{0} submit 5 to press the letter when the last timer digit is 5.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        parameters = Command.Trim().ToUpperInvariant().Split(' ');
        if (parameters.Length > 2 || parameters.Length < 1)
        {
            yield break;
        }
        else if (new string[] { "LEFT", "RIGHT", "L", "R" }.Contains(parameters[0]))
        {
            if (parameters.Length == 1)
            {
                WhichButton(parameters[0]).OnInteract();
            }
            else
            {
                if (parameters[1].All(x => "0123456789".Contains(x)) && int.Parse(parameters[1]) <= 50) //Checks if the number is actually a number, and checks that the number is not comically large.
                {
                    yield return null;
                    for (int i = 0; i < int.Parse(parameters[1]); i++)
                    {
                        WhichButton(parameters[0]).OnInteract();
                        yield return new WaitForSeconds(0.75f);
                        yield return "trycancel";
                    }
                }
            }
        }
        else if (parameters[0] == "SUBMIT" && parameters.Length == 2)
        {

            if ("0123456789".Contains(parameters[1]))
            {
                yield return null;
                int submissionDigit = int.Parse(parameters[1]);
                while (Mathf.FloorToInt(Bomb.GetTime()) % 10 == submissionDigit)
                {
                    yield return "trycancel"; //Fixes that obscure bug that got square button on ON.
                }
                while (Mathf.FloorToInt(Bomb.GetTime()) % 10 != submissionDigit)
                {
                    yield return "trycancel";
                }
                submitButton.OnInteract();

            }
        }
    }
   
    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            while (Mathf.FloorToInt(Bomb.GetTime()) % 10 != solution)
            {
                yield return true;
            }
            submitButton.OnInteract();
            yield return new WaitForSeconds(0.05f);
        }
    }
}
