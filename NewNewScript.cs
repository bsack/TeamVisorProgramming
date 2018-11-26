using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics; //stopwatch is in here
using UnityEngine.Windows.Speech;
using System.Linq;
using System.Text;
using System.Collections;
//using System.Threading;
//using System.Net;
//using System.IO;

[Serializable] //makes it possible to go from JSON to object, variables below must match what is in JSON data

public class TelemetryData
{

    public float press;       // Internal suit pressure
    public float temp;       // Internal suit temperature
    public double o2 = 100;         // Oxygen levels
    public double battery = 100;          // Battery life remaining
    public string t_eva;
    /*public double rate_o2;     // OXYGEN RATE [psi/min] - Flowrate of the Primary Oxygen Pack - Expected range is from 0.5 to 1 psi/min
    public float cap_battery; // BATTERY CAPACITY [amp-hr] - Total capacity of the spacesuit’s battery - Expected range is from 0 to 30 amp-hr
    public float p_h2o_g;     // H2O GAS PRESSURE [psia] - Gas pressure from H2O system - Expected range is from 14 to 16 psia
    public float p_h2o_l;     // H2O LIQUID PRESSURE [psia] - Liquid pressure from H2O system - Expected range is from 14 to 16 psia
    public float p_sop;       // SOP PRESSURE [psia] - Pressure inside the Secondary Oxygen Pack - Expected range is from 750 to 950 psia.
    public float rate_sop;    // SOP RATE [psi/min] - Flowrate of the Secondary Oxygen Pack - Expected range is from 0.5 to 1 psi/min.*/



    public string instructions;
}

public class DisplayedTelemetryFields
{

    public Boolean press;
    public Boolean temp;
    public Boolean o2;
    public Boolean t_eva;
    public Boolean battery;
    /*public Boolean p_o2;
    public Boolean rate_o2;
    public Boolean cap_battery;
    public Boolean p_h2o_g;
    public Boolean p_h2o_l;
    public Boolean p_sop;
    public Boolean rate_sop;*/
    public Boolean instructions;
}

[Serializable] //same thing as before when you get data, serialize into this class (as if its correctly doing T/F) ---fix to be correct in app!!!! (remove quotes)
public class SwitchData
{
    public Boolean sop_on;
    public Boolean sspe;
    public Boolean fan_error;
    public Boolean vent_error;
    public Boolean vehicle_power;
    public Boolean h2o_off;
    public Boolean o2_off;
    //must add alarm variables
}

public class NewNewScript : MonoBehaviour
{

    [SerializeField]

    public int instruction_number = 1;
    int i = 0; //instruction step counter

    Stopwatch stopWatch = new Stopwatch();  // This will track elapsed time for updates
    TimeSpan bat_last_reset;                // TimeSpan value to track when battery was last reset to full capacity
    TimeSpan o2_last_reset;

    //Text telemetryBox; //declaring text object to be used later to set equal to the "Telemetry" object we find
    Text press, temp, o2, t_eva, battery, instructions; // p_o2, rate_o2, cap_battery, p_h2o_g, p_h2o_l, p_sop, rate_sop;
    Text alertsBox;

    public TelemetryData telemetrydata = new TelemetryData(); //telemtrydata is a new instance of the TelemetryData Class above (whats gonna hold values from stream)
    //public TelemetryData batdata = new TelemetryData();
    SwitchData switchdata = new SwitchData();
    DisplayedTelemetryFields displayedtelemetryfields = new DisplayedTelemetryFields();

    

    

    //float elapsedSecs = 0.0f;           // Overall counter (currently not used)
    static float refreshSecs = 1.0f;    // How often to refresh (GET) the data from the telemetry server (seconds) - used later to do a refresh in the beginning, must be static cuz its initial
    float refreshTimer = refreshSecs;   // Timer used to trigger a data refresh (initialized to match so we immediately refresh) - used for refresh after

    static TimeSpan t_max = new TimeSpan(0, 0, 2, 1);    // Initialize max eva time struct (days, hours, mins, secs) (used to calculate time remaining) - need to add one second to whatever you want
    static TimeSpan bat_max = new TimeSpan(0, 0, 3, 1);  // Initialize max battery time struct (days, hours, mins, secs) (ie how long a full battery lasts) - need to add one second to whatever you want
    static TimeSpan o2_max = new TimeSpan(0, 0, 4, 1);

    DisplayRandomInstructions display = new DisplayRandomInstructions();

    //instantiate all the pictures we will use
    public Image myImageComponent;
    public Image myBoxedImageComponent;
    public Sprite[] sprite_array;
    public Sprite[] boxed_images;
    //Dictionary<string, Sprite> sprite_dict = new Dictionary<string, Sprite>();
    



    // Use this for initialization
    void Start()
    {
        UnityEngine.Debug.Log("Initializing ..."); //message to console to tell its running

        display.Generate();

        //myImageComponent = GetComponent<Image>();

        stopWatch.Start(); //starts the timer
        bat_last_reset = stopWatch.Elapsed;  // to recharge the battery again later, set it to stopWatch.Elapsed and it will start to count down again
        o2_last_reset = stopWatch.Elapsed;

        // Find the "Telemetry" Text canvas object and assign it to telemetryBox
        // The position, initial message, positioning, font, etc are all defined in the Unity Inspector
        if (press == null) //if we havent assigned anything to telemetryBox and havent found Telemetry then... 
        {

            GameObject child = GameObject.Find("Pressure");
            press = child.GetComponent<Text>();

            child = GameObject.Find("Temperature");
            temp = child.GetComponent<Text>();

            child = GameObject.Find("Oxygen");
            o2 = child.GetComponent<Text>();

            child = GameObject.Find("EVA Time Elapsed");
            t_eva = child.GetComponent<Text>();

            child = GameObject.Find("Battery");
            battery = child.GetComponent<Text>();

            child = GameObject.Find("Instructions");
            instructions = child.GetComponent<Text>();
        }


            
       

        //Setting all of the boolean metric variables to be true to display everything
        displayedtelemetryfields.press = true;
        displayedtelemetryfields.temp = true;
        displayedtelemetryfields.o2 = true;
        displayedtelemetryfields.t_eva = true;
        displayedtelemetryfields.battery = true;
        displayedtelemetryfields.instructions = true;

    }

    // Update is called once per frame
    void Update()
    {
        // Get the elapsed time as a type TimeSpan value.
        TimeSpan ts_elap = stopWatch.Elapsed; //stopwatch.elapsed returns the time since you started the stopwatch as a timespan value

        // Calculate the remaining time as a TimeSpan value
        //TimeSpan ts_rem = t_max.Subtract(ts_elap)
        TimeSpan ts_rem = (t_max.Subtract(ts_elap)).Duration(); // Takes t_max from above and subtracts time elapsed, Duration() converts it to an absolute value. This will be negative when it hits 0

        // Determine remaining battery life (as a TimeSpan) (basically max battery life minus the time since the battery was last reset to full charge) 
        TimeSpan bat_rem = bat_max.Subtract(ts_elap.Subtract(bat_last_reset));
        if (bat_rem.TotalSeconds < 0) { bat_rem = new TimeSpan(0, 0, 0, 0); }  // if it goes negative, set it to a zero TimeSpan

        TimeSpan o2_rem = o2_max.Subtract(ts_elap.Subtract(o2_last_reset));
        if (o2_rem.TotalSeconds < 0) { o2_rem = new TimeSpan(0, 0, 0, 0); }


        // Format and display the TimeSpan value.  (do this every update (vs inside the refeshTimer check) so times update often)
        //string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}:{3:00}.{4:00}",
        //    ts_elap.Days, ts_elap.Hours, ts_elap.Minutes, ts_elap.Seconds,
        //    ts_elap.Milliseconds / 10);
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts_elap.Hours, ts_elap.Minutes, ts_elap.Seconds); //make a string variable called elapsedTime. Says # of variable:format, so the first thing (0) is hours formatted as 00, : (this is the colon we see in the time), the second (1) is minutes formatted 00 and so on. This allows you to avoid doing math and just put in the time you want conveniently
                                                                                                                     // t_eva.text = "Time Elapsed: " + elapsedTime; // give this to t_eva text object so we can put it on the screen

       

        // Track overall elapsed time
        //elapsedSecs += Time.deltaTime;

        // Increment timer with time elapsed since the last update
        refreshTimer += Time.deltaTime; //time delta is basically framerate (how often update happens)

        if (refreshTimer > refreshSecs)
        {
            System.Random r = new System.Random();

            refreshTimer = 0; //reset the timer
            //telemetrydata = RefreshData(); //refresh the data
            telemetrydata.battery = Math.Round(DivideTimeSpans(bat_rem, bat_max) * 100, 0);  // Remaining battery life as an integer % of max battery life
            telemetrydata.o2 = Math.Round(DivideTimeSpans(o2_rem, o2_max) * 100, 0);  // Remaining o2 life as an integer % of max o2 life
            telemetrydata.t_eva = elapsedTime;
            telemetrydata.press = r.Next(3, 5);
            telemetrydata.temp = r.Next(60, 80);


            press.text = "Suit Pressure: " + telemetrydata.press.ToString() + " psi";
            temp.text = "Suit Temperature: " + telemetrydata.temp.ToString() + " °F";
            o2.text = "Oxygen Level: " + telemetrydata.o2.ToString() + "%";
            t_eva.text = "Time Elapsed: " + telemetrydata.t_eva.ToString();
            battery.text = "Battery Life:  " + telemetrydata.battery.ToString() + "%";

            // Update displayed telemetry data
            //List<string> metrics = new List<string>(); //defining a new class type list and calling it metrics, it will be a list of strings

        }

        /*
        if (Input.GetKeyDown(KeyCode.RightArrow))  //replace with voice command if
        {
            i++;
            if (i > 19) { i = 19; }
            instructions.text = instructions_set[i];

        }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) //replace with voice command if
        {
            i--;
            if (i < 0) { i = 0; }
            instructions.text = instructions_set[i];
        } */
    }

    public void NextStep()
    {
        
        InstructionAndPicture instructionChosen = new InstructionAndPicture();
        instructionChosen = display.nextInstruction(instruction_number);

        //Set Instruction
        GameObject child = GameObject.Find("Instructions");
        Text instructions = child.GetComponent<Text>();
        instructions.text = instructionChosen.instruction;

        //Set Picture
        myImageComponent.sprite = sprite_array[instructionChosen.pic_index];
        myBoxedImageComponent.sprite = boxed_images[instructionChosen.boxpic_index];
        instruction_number = instruction_number + 1;

    }

    

    /*
    public TelemetryData RefreshData()
    {
        TelemetryData up = new TelemetryData();
        System.Random r = new System.Random();

        up.press = r.Next(3, 5);
        up.temp = r.Next(60, 80);

        //up.o2 = r.Next(10000, 40000);
        //up.battery = r.Next(750, 950);
        /*up.rate_o2 = r.Next(5, 10) / 10;
        up.cap_battery = r.Next(0, 30);
        up.p_h2o_g = r.Next(14, 16);
        up.p_h2o_l = r.Next(14, 16);
        up.p_sop = r.Next(750, 950);
        up.rate_sop = r.Next(5, 10) / 10;
        

        return up;
    } */

    public static double DivideTimeSpans(TimeSpan dividend, TimeSpan divisor)
    {
        return (double)dividend.Ticks / (double)divisor.Ticks;
    }
}


public class DisplayRandomInstructions
{
    int instructionNum = 1;

    ArrayList categories = new ArrayList();

    ArrayList flipSwitch = new ArrayList();
    ArrayList pushButton = new ArrayList();
    //ArrayList tether = new ArrayList();
    ArrayList turnKnob = new ArrayList();
    ArrayList pluglist = new ArrayList();

    Dictionary<string, ArrayList> instruct_dict = new Dictionary<string, ArrayList>();

    public void Generate()
    {
        CreateCategories();
        createAndAddInstructionAndPictures();
        createInstructionDictionary();
    }

    private void CreateCategories()
    {
        categories.Add("FlipSwitch");
        categories.Add("PushButton");
        //categories.Add("Tether");
        categories.Add("Plug");
        categories.Add("Knob");
        //categories.Add("Screw");

    }

    private void createAndAddInstructionAndPictures()
    {
        //hard coded creation of every pair of instructions and picture pairs and addition to their category's arraylist

        // Flip Switch (10 instructions)
        InstructionAndPicture switch1A = new InstructionAndPicture();
        switch1A.instruction = "Flip switch A1.";
        switch1A.pic_index = 8;
        switch1A.boxpic_index = 25;
        flipSwitch.Add(switch1A);

        InstructionAndPicture switch1B = new InstructionAndPicture();
        switch1B.instruction = "Flip switch B1.";
        switch1B.pic_index = 7;
        switch1B.boxpic_index = 30;
        flipSwitch.Add(switch1B);

        InstructionAndPicture switch2A = new InstructionAndPicture();
        switch2A.instruction = "Flip switch A2.";
        switch2A.pic_index = 8;
        switch2A.boxpic_index = 26;
        flipSwitch.Add(switch2A);

        InstructionAndPicture switch2B = new InstructionAndPicture();
        switch2B.instruction = "Flip switch B2.";
        switch2B.pic_index = 7;
        switch2B.boxpic_index = 31;
        flipSwitch.Add(switch2B);

        InstructionAndPicture switch3A = new InstructionAndPicture();
        switch3A.instruction = "Flip switch A3.";
        switch3A.pic_index = 8;
        switch3A.boxpic_index = 27;
        flipSwitch.Add(switch3A);

        InstructionAndPicture switch3B = new InstructionAndPicture();
        switch3B.instruction = "Flip switch B3.";
        switch3B.pic_index = 7;
        switch3B.boxpic_index = 32;
        flipSwitch.Add(switch3B);

        InstructionAndPicture switch4A = new InstructionAndPicture();
        switch4A.instruction = "Flip switch A4.";
        switch4A.pic_index = 8;
        switch4A.boxpic_index = 28;
        flipSwitch.Add(switch4A);

        InstructionAndPicture switch4B = new InstructionAndPicture();
        switch4B.instruction = "Flip switch B4.";
        switch4B.pic_index = 7;
        switch4B.boxpic_index = 33;
        flipSwitch.Add(switch4B);

        InstructionAndPicture switch5A = new InstructionAndPicture();
        switch5A.instruction = "Flip switch A5.";
        switch5A.pic_index = 8;
        switch5A.boxpic_index = 29;
        flipSwitch.Add(switch5A);

        InstructionAndPicture switch5B = new InstructionAndPicture();
        switch5B.instruction = "Flip switch B5.";
        switch5B.pic_index = 7;
        switch5B.boxpic_index = 34;
        flipSwitch.Add(switch5B);

        // Push Button (10)
        InstructionAndPicture buttonRed1 = new InstructionAndPicture();
        buttonRed1.instruction = "Push button Red One.";
        buttonRed1.pic_index = 0;
        buttonRed1.boxpic_index = 0;
        pushButton.Add(buttonRed1);

        InstructionAndPicture buttonRed2 = new InstructionAndPicture();
        buttonRed2.instruction = "Push button Red Two.";
        buttonRed2.pic_index = 0;
        buttonRed2.boxpic_index = 1;
        pushButton.Add(buttonRed2);

        InstructionAndPicture buttonRed3 = new InstructionAndPicture();
        buttonRed3.instruction = "Push button Red Three.";
        buttonRed3.pic_index = 0;
        buttonRed3.boxpic_index = 2;
        pushButton.Add(buttonRed3);

        InstructionAndPicture buttonRed4 = new InstructionAndPicture();
        buttonRed4.instruction = "Push button Red Four.";
        buttonRed4.pic_index = 0;
        buttonRed4.boxpic_index = 3;
        pushButton.Add(buttonRed4);

        InstructionAndPicture buttonRed5 = new InstructionAndPicture();
        buttonRed5.instruction = "Push button Red Five.";
        buttonRed5.pic_index = 0;
        buttonRed5.boxpic_index = 4;
        pushButton.Add(buttonRed5);

        InstructionAndPicture buttonOrange1 = new InstructionAndPicture();
        buttonOrange1.instruction = "Push button Orange One.";
        buttonOrange1.pic_index = 3;
        buttonOrange1.boxpic_index = 5;
        pushButton.Add(buttonOrange1);

        InstructionAndPicture buttonO2 = new InstructionAndPicture();
        buttonO2.instruction = "Push button Orange Two.";
        buttonO2.pic_index = 3;
        buttonO2.boxpic_index = 6;
        pushButton.Add(buttonO2);

        InstructionAndPicture buttonO3 = new InstructionAndPicture();
        buttonO3.instruction = "Push button Orange Three.";
        buttonO3.pic_index = 3;
        buttonO3.boxpic_index = 7;
        pushButton.Add(buttonO3);

        InstructionAndPicture buttonO4 = new InstructionAndPicture();
        buttonO4.instruction = "Push button Orange Four.";
        buttonO4.pic_index = 3;
        buttonO4.boxpic_index = 8;
        pushButton.Add(buttonO4);

        InstructionAndPicture buttonO5 = new InstructionAndPicture();
        buttonO5.instruction = "Push button Orange Five.";
        buttonO5.pic_index = 3;
        buttonO5.boxpic_index = 9;
        pushButton.Add(buttonO5);

        InstructionAndPicture buttonG1 = new InstructionAndPicture();
        buttonG1.instruction = "Push button Gray One.";
        buttonG1.pic_index = 2;
        buttonG1.boxpic_index = 10;
        pushButton.Add(buttonG1);

        InstructionAndPicture buttonG2 = new InstructionAndPicture();
        buttonG2.instruction = "Push button Gray Two.";
        buttonG2.pic_index = 2;
        buttonG2.boxpic_index = 11;
        pushButton.Add(buttonG2);

        InstructionAndPicture buttonG3 = new InstructionAndPicture();
        buttonG3.instruction = "Push button Gray Three.";
        buttonG3.pic_index = 2;
        buttonG3.boxpic_index = 12;
        pushButton.Add(buttonG3);

        InstructionAndPicture buttonG4 = new InstructionAndPicture();
        buttonG4.instruction = "Push button Gray Four.";
        buttonG4.pic_index = 2;
        buttonG4.boxpic_index = 13;
        pushButton.Add(buttonG4);

        InstructionAndPicture buttonG5 = new InstructionAndPicture();
        buttonG5.instruction = "Push button Gray Five.";
        buttonG5.pic_index = 2;
        buttonG5.boxpic_index = 14;
        pushButton.Add(buttonG5);

        InstructionAndPicture buttonGr1 = new InstructionAndPicture();
        buttonGr1.instruction = "Push button Green One.";
        buttonGr1.pic_index = 4;
        buttonGr1.boxpic_index = 15;
        pushButton.Add(buttonGr1);

        InstructionAndPicture buttonGr2 = new InstructionAndPicture();
        buttonGr2.instruction = "Push button Green Two.";
        buttonGr2.pic_index = 4;
        buttonGr2.boxpic_index = 16;
        pushButton.Add(buttonGr2);

        InstructionAndPicture buttonGr3 = new InstructionAndPicture();
        buttonGr3.instruction = "Push button Green Three.";
        buttonGr3.pic_index = 4;
        buttonGr3.boxpic_index = 17;
        pushButton.Add(buttonGr3);

        InstructionAndPicture buttonGr4 = new InstructionAndPicture();
        buttonGr4.instruction = "Push button Green Four.";
        buttonGr4.pic_index = 4;
        buttonGr4.boxpic_index = 18;
        pushButton.Add(buttonGr4);

        InstructionAndPicture buttonGr5 = new InstructionAndPicture();
        buttonGr5.instruction = "Push button Green Five.";
        buttonGr5.pic_index = 4;
        buttonGr5.boxpic_index = 19;
        pushButton.Add(buttonGr5);

        InstructionAndPicture buttonB1 = new InstructionAndPicture();
        buttonB1.instruction = "Push button Blue One.";
        buttonB1.pic_index = 1;
        buttonB1.boxpic_index = 20;
        pushButton.Add(buttonB1);

        InstructionAndPicture buttonB2 = new InstructionAndPicture();
        buttonB2.instruction = "Push button Blue Two.";
        buttonB2.pic_index = 1;
        buttonB2.boxpic_index = 21;
        pushButton.Add(buttonB2);

        InstructionAndPicture buttonB3 = new InstructionAndPicture();
        buttonB3.instruction = "Push button Blue Three.";
        buttonB3.pic_index = 1;
        buttonB3.boxpic_index = 22;
        pushButton.Add(buttonB3);

        InstructionAndPicture buttonB4 = new InstructionAndPicture();
        buttonB4.instruction = "Push button Blue Four.";
        buttonB4.pic_index = 1;
        buttonB4.boxpic_index = 23;
        pushButton.Add(buttonB4);

        InstructionAndPicture buttonB5 = new InstructionAndPicture();
        buttonB5.instruction = "Push button Blue Five.";
        buttonB5.pic_index = 1;
        buttonB5.boxpic_index = 24;
        pushButton.Add(buttonB5);

        // Tether (8 - in order)

        // Plug (2 - in order)
        InstructionAndPicture plug = new InstructionAndPicture();
        plug.instruction = "Insert plug into socket";
        plug.pic_index = 6;
        plug.boxpic_index = 41;
        pluglist.Add(plug);

        InstructionAndPicture unplug = new InstructionAndPicture();
        unplug.instruction = "Remove plug from socket";
        unplug.pic_index = 6;
        unplug.boxpic_index = 41;
        pluglist.Add(unplug);

        // Turn Knob (10)
        InstructionAndPicture knob1 = new InstructionAndPicture();
        knob1.instruction = "Turn dial 1 to ";
        knob1.pic_index = 9;
        knob1.boxpic_index = 35;
        turnKnob.Add(knob1);

        InstructionAndPicture knob2 = new InstructionAndPicture();
        knob2.instruction = "Turn dial 2 to ";
        knob2.pic_index = 9;
        knob2.boxpic_index = 36;
        turnKnob.Add(knob2);

        InstructionAndPicture knob3 = new InstructionAndPicture();
        knob3.instruction = "Turn dial 3 to ";
        knob3.pic_index = 9;
        knob3.boxpic_index = 37;
        turnKnob.Add(knob3);

        InstructionAndPicture knob4 = new InstructionAndPicture();
        knob4.instruction = "Turn dial 4 to ";
        knob4.pic_index = 9;
        knob4.boxpic_index = 38;
        turnKnob.Add(knob4);

        InstructionAndPicture knob5 = new InstructionAndPicture();
        knob5.instruction = "Turn dial 5 to ";
        knob5.pic_index = 9;
        knob5.boxpic_index = 39;
        turnKnob.Add(knob5);
    }


    private void createInstructionDictionary()
    {
        // HashMap<String, ArrayList> where
        // String (key) => ArrayList<StringAndPicture> (value) that has the individual instructions

        //Add all categories and their arraylist of instructions to the Dictionary

        instruct_dict.Add("FlipSwitch", flipSwitch) /* ArriayList<InstructionAndPicture>*/;
        instruct_dict.Add("PushButton", pushButton);
        //instruct_dict.Add("Tether", tether);
        instruct_dict.Add("Knob", turnKnob);
        instruct_dict.Add("Plug", pluglist);

    }

    // Choose a random instruction and display it on the screen
    public InstructionAndPicture nextInstruction(int instructionNum)
    {
        /*foreach (var item in turnKnob)
        {
            UnityEngine.Debug.Log("The nodes of MDG are:" + item.instruction);
        } */
        //UnityEngine.Debug.Log(turnKnob.ToString());
        InstructionAndPicture instructionChosen = null;
        ArrayList instructionArrayList = new ArrayList();
        string categoryChosen = null;
        // first instruction: open the box
        if (instructionNum == 1)
        {
            InstructionAndPicture openBox = new InstructionAndPicture
            {
                instruction = "Unscrew fastners and remove lid from the gray box.",
                pic_index = 5
            };
            instructionChosen = openBox;
        }
        else if (instructionNum == 2)
        {
            InstructionAndPicture takeCarabiners = new InstructionAndPicture
            {
                instruction = "Remove carabiners from box. Place them aside.",
                pic_index = 10
            };
            instructionChosen = takeCarabiners;
        }
        else if (instructionNum == 49)
        {
            InstructionAndPicture returnCarabiners = new InstructionAndPicture
            {
                instruction = "Return carabiners to gray box",
                pic_index = 10
            };
            instructionChosen = returnCarabiners;
        } //last instruction: close the box//last instruction: close the box//last instruction: close the box
        else if (instructionNum == 50)
        {
            InstructionAndPicture closeBox = new InstructionAndPicture
            {
                instruction = "Replace lid and screw in fastners.",
                pic_index = 5
            };
            instructionChosen = closeBox;
        }
        else
        {
            // Randomly choose from the 6 categories using math.random % categories.size()
            System.Random rnd = new System.Random();
            categoryChosen = (string) categories[rnd.Next(categories.Count)];
            UnityEngine.Debug.Log(categoryChosen);


            if (categoryChosen != "Plug" && categoryChosen != "Knob")
            {
                ArrayList value;
                // Use math.random() % arraylist.size() to pick an instruction
                if (instruct_dict.TryGetValue(categoryChosen, out value))
                {
                    instructionArrayList = value;

                    instructionChosen = (InstructionAndPicture) instructionArrayList[rnd.Next(instructionArrayList.Count)];
                    
                }
            }
            else if(categoryChosen == "Plug")
            {
                ArrayList value;
                if (instruct_dict.TryGetValue(categoryChosen, out value))
                {
                    instructionArrayList = value;
                    instructionChosen = (InstructionAndPicture) instructionArrayList[0];
                    
                }
            }
            else if(categoryChosen == "Knob")
            {
                ArrayList value;
                System.Random r = new System.Random();
               
                if (instruct_dict.TryGetValue(categoryChosen, out value))
                {
                    instructionArrayList = value;

                    int temp_num = rnd.Next(instructionArrayList.Count);
                    int len = instructionArrayList.Count;
                    instructionChosen = (InstructionAndPicture) instructionArrayList[temp_num];
                    UnityEngine.Debug.Log(temp_num);
                    UnityEngine.Debug.Log(len);
                    UnityEngine.Debug.Log(instructionChosen.instruction);
                    instructionChosen.instruction += r.Next(0, 100) + ".";
                    UnityEngine.Debug.Log(instructionChosen.instruction);
                }
            }
        }

        if (instructionNum != 1 && instructionNum != 50 && instructionNum != 2 && instructionNum != 49)
        {
            // Delete from arraylist after use
            instructionArrayList.Remove(instructionChosen);

            // If it’s arraylist is now empty
            ArrayList test;
            if (instruct_dict.TryGetValue(categoryChosen, out test))
            {
                instructionArrayList = test;
                if (test.Count == 0)
                {
                    categories.Remove(categoryChosen);
                    UnityEngine.Debug.Log("Category successfully removed");
                }
            }
        }
        return instructionChosen;
    }
}


public class InstructionAndPicture
{
    public string instruction;
    public int pic_index;
    public int boxpic_index;

    public void setInstruction(string instruction)
    {
        this.instruction = instruction;
    }

    public void setPicFilePath(int pic)
    {
        this.pic_index = pic;
    }

    public string getInstruction()
    {
        return instruction;
    }

    public int getPicFilePath()
    {
        return pic_index;
    }
}
