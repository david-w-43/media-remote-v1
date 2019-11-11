/*
   USB Serial Communicator v1.3

   This program takes input from five PTM switches and a clickable rotary encoder to control
    - Next
    - Previous
    - Repeat mode
    - Shuffle toggle
    - Volume
    - Pause toggle
    - Modifier

   It outputs the command received over serial, intended for the command relay program.
   It will also receive information over this serial connection and display relevant
   information on the LCD display. When the modifier switch is pressed

                                        ^^^^
                                       SERIAL
                                       +-----+
          +----[PWR]-------------------| USB |--+
          |                            +-----+  |
          |         GND/RST2  [ ][ ]            |
          |       MOSI2/SCK2  [ ][ ]  A5/SCL[X] |    LCD SCL
          |          5V/MISO2 [ ][ ]  A4/SDA[X] |    LCD SDA
          |                             AREF[ ] |
          |                              GND[X] |
          | [ ]N/C                    SCK/13[ ] |
          | [ ]IOREF                 MISO/12[X] |    PTM1 (Next)
          | [ ]RST                   MOSI/11[X]~|    PTM2 (Prev)
          | [ ]3V3    +---+               10[X]~|    PTM3 (Shuffle)
          | [X]5V    -| A |-               9[X]~|    PTM4 (Repeat)
          | [ ]GND   -| R |-               8[X] |    Enc_SW (Pause)
          | [ ]GND   -| D |-                    |
          | [ ]Vin   -| U |-               7[X] |    PTM5 (Modifier)
          |          -| I |-               6[X]~|    LCD Backlight
          | [ ]A0    -| N |-               5[ ]~|
          | [ ]A1    -| O |-               4[ ] |
          | [ ]A2     +---+           INT1/3[X]~|    Enc_CLK
          | [ ]A3                     INT0/2[X] |    Enc_DT
          | [ ]A4/SDA  RST SCK MISO     TX>1[ ] |
          | [ ]A5/SCL  [ ] [ ] [ ]      RX<0[ ] |
          |            [ ] [ ] [ ]              |
          |  UNO_R3    GND MOSI 5V  ____________/
           \_______________________/

*/

//-----------------------------------------------------------------------

#define ENCODER_OPTIMIZE_INTERRUPTS //Optimise encoder performance
//Libraries required
#include <LiquidCrystal_I2C.h>
#include <Encoder.h>

//-----------------------------------------------------------------------

//Serial settings
const int BAUD_RATE = 19200;
const int TIMEOUT   =  200;

//LCD settings
const int ROWS = 4;
const int COLS = 20;

//Pin settings
const int PIN_NEXT = 12;
const int PIN_PREV = 9;
const int PIN_SHUF = 11;
const int PIN_REPT =  10;
const int PIN_PAUS =  8;
const int PIN_MODIFIER = 7;
const int PIN_BACKLIGHT = 6;

const int ENC_CLK  =  3;
const int ENC_DT   =  2;

//Volume settings
const int UPPERBOUND  = 125; //125 for VLC
const int LOWERBOUND  =   0;
const int DEF_VOL = 100;

//Number of steps per click
const int STEPS_PER_CLICK = 4;

//Backlight
int backlightLevel = 4;
const int BACKLIGHT_STEP = 255 / 8;

//Checks if connected
bool isConnected = false;

//Custom characters
//Ellipsis
const byte ellipsis[] = {
  B00000,
  B00000,
  B00000,
  B00000,
  B00000,
  B00000,
  B10101,
  B00000
};

//First half of shuffle icon
const byte shuff1[] = {
  B00000,
  B01100,
  B00010,
  B00001,
  B00010,
  B01100,
  B00000,
  B00000
};

//Second half of shuffle icon
const byte shuff2[] = {
  B00010,
  B01111,
  B10010,
  B00000,
  B10010,
  B01111,
  B00010,
  B00000
};

//First half of repeat icon - common between repeat modes
const byte repeat1[] = {
  B00000,
  B00111,
  B01000,
  B00000,
  B00000,
  B00100,
  B01111,
  B00100
};

//Second half of repeat all
const byte repeat2[] = {
  B00100,
  B11110,
  B00100,
  B00000,
  B00000,
  B00100,
  B11000,
  B00000
};

//Second half of repeat single
const byte repeat3[] = {
  B00100,
  B11110,
  B00100,
  B00000,
  B00001,
  B00101,
  B11001,
  B00001
};

//First half of volume icon
const byte speaker[] = {
  B00000,
  B00001,
  B00011,
  B01111,
  B01111,
  B00011,
  B00001,
  B00000
};

//Second half of volume icon
const byte volume3[] = {
  B00010,
  B01001,
  B00101,
  B10101,
  B10101,
  B00101,
  B01001,
  B00010
};



//-----------------------------------------------------------------------

//Initialisation
LiquidCrystal_I2C lcd(0x27, COLS, ROWS); //Define 'lcd', 20x4 character display
Encoder encoder(ENC_CLK, ENC_DT); //Define 'encoder', running on the interrupt pins

//-----------------------------------------------------------------------

//Define structure for the metadata container and playback parameters
struct metas {
  String title;
  String artist;
  unsigned int timeLength;
} metadata;

struct params {
  //bool isPlaying;
  unsigned int currentTime;
  unsigned int currentVolume;
  bool changed;
  bool shuffle;
  int repeatMode; //0 - None, 1 - Loop, 2 - Repeat
} playback;

//-----------------------------------------------------------------------

void setup() {
  //Begin serial communication with specified settings
  Serial.begin(BAUD_RATE);
  Serial.setTimeout(TIMEOUT);

  //Define pins
  pinMode(PIN_NEXT, INPUT_PULLUP);
  pinMode(PIN_PREV, INPUT_PULLUP);
  pinMode(PIN_SHUF, INPUT_PULLUP);
  pinMode(PIN_REPT, INPUT_PULLUP);
  pinMode(PIN_PAUS, INPUT_PULLUP);
  pinMode(PIN_MODIFIER, INPUT_PULLUP);
  pinMode(PIN_BACKLIGHT, OUTPUT);

  //Initialise LCD, turn on backlight
  lcd.init();
  lcd.backlight();

  //Define custom characters
  lcd.createChar(1, ellipsis); //Character 1 is ellipsis
  lcd.createChar(2, shuff1);   //Character 2 is first half of shuffle
  lcd.createChar(3, shuff2);   //Second half of shuffle
  lcd.createChar(4, repeat1);  //First half of repeat
  lcd.createChar(5, repeat2);  //Second half of repeat
  lcd.createChar(6, repeat3);  //Second half of repeat1
  lcd.createChar(7, speaker);  //Speaker icon
  lcd.createChar(8, volume3);  //Volume waves

  //Display startup message
  showStartupMessage();
  //Set backlight
  analogWrite(PIN_BACKLIGHT, round(backlightLevel * BACKLIGHT_STEP));


  //Set volume to default, for now
  encoder.write(DEF_VOL);

  //FOR TESTING PURPOSES---------------------------------------------------
  //  metadata.title = "Big Iron";
  //  metadata.artist = "Marty Robbins";
  //  metadata.album = "Gunfighter Ballads and Trail Songs";
  //  metadata.timeLength = 235;
  //  //playback.isPlaying = true;
  //  playback.currentTime = 102;
  //  playback.currentVolume = 256;

  //  encoder.write(playback.currentVolume);
  //  playback.changed = true;
  //-----------------------------------------------------------------------

}

//-----------------------------------------------------------------------

//Initialise previous value variables as false
bool oldState_NEXT = false;
bool oldState_PREV = false;
bool oldState_SHUF = false;
bool oldState_REPT = false;
bool oldState_PAUS = false;

void serialEvent() {                                                     //When data received over serial
  Serial.flush();                                                        //Wait for outgoing comms to finish
  String receivedData = Serial.readString();                             //Read the data into one string
  unsigned int startPosition, endPosition;

  receivedData.trim();                                                   //Trim whitespace
  if (receivedData.indexOf("TIME:") != -1) {
    
    startPosition = receivedData.indexOf("\n", receivedData.indexOf("TIME:")) + 1;                   //Find the start position of the time
    endPosition = receivedData.indexOf("\n", startPosition);             //Find the end of the time
    playback.currentTime = receivedData.substring(startPosition, endPosition).toInt(); //Set time to substring

    displayTime();
    
  }
  if (receivedData.indexOf("META:") != -1) {                                //If string contains metadata (also new file)
    
    startPosition = receivedData.indexOf("\n", receivedData.indexOf("META:")) + 1;                   //Find the start position of the title
    endPosition = receivedData.indexOf("\n", startPosition);             //Find the end of the title
    metadata.title = receivedData.substring(startPosition, endPosition); //Set title to substring

    startPosition = receivedData.indexOf("\n", endPosition) + 1;         //Find the start position of the artist
    endPosition = receivedData.indexOf("\n", startPosition);             //Find the end of the artist
    metadata.artist = receivedData.substring(startPosition, endPosition);//Set artist to substring

    startPosition = receivedData.indexOf("\n", endPosition) + 1;  //Track length
    endPosition = receivedData.indexOf("\n", startPosition);
    metadata.timeLength = receivedData.substring(startPosition, endPosition).toInt();

    displayMetadata();
    displayTime();
  }
   if (receivedData.indexOf("PARA:") != -1) {
    Serial.println("received PARA");

    //Reset vars
    playback.repeatMode = 0;
    playback.shuffle = false;


    startPosition = receivedData.indexOf("\n", receivedData.indexOf("PARA:")) + 1;                   //Find the start position of the volume
    endPosition = receivedData.indexOf("\n", startPosition);             //Find the end of the volume


    startPosition = receivedData.indexOf("\n", endPosition) + 1;         //Find the start position of the shuffle state
    endPosition = receivedData.indexOf("\n", startPosition);             //Find the end of the shuffle state
    String temp = receivedData.substring(startPosition, endPosition);    //Set temp variable to substring
    if (temp == "True") {
      playback.shuffle = true;
    }
    if (temp == "False") {
      playback.shuffle = false;
    }
    Serial.println(String("SHUFFLE: " + String(temp)));

    startPosition = receivedData.indexOf("\n", endPosition) + 1;
    endPosition = receivedData.indexOf("\n", startPosition);
    playback.repeatMode = receivedData.substring(startPosition, endPosition).toInt();
    Serial.println(String("MODE: " + String(playback.repeatMode)));

    displayRepeatShuffle();
    displayVolume();
  }
  if (receivedData.indexOf("HANDSHAKE") != -1) { //To indicate to the Data Relay that it has found the right device
    Serial.println("SHAKEN");
    lcd.clear();
    displayVolume();
    isConnected = true;
  }
   if (receivedData.indexOf("EXIT") != -1) {
    showStartupMessage();
    isConnected = false;
  }
   if (receivedData.indexOf("VOL:") != -1) {
    startPosition = receivedData.indexOf("\n", receivedData.indexOf("VOL:")) + 1;                   //Find the start position of the volume
    endPosition = receivedData.indexOf("\n", startPosition);             //Find the end of the volume
    playback.currentVolume = receivedData.substring(startPosition, endPosition).toInt();
    encoder.write(round(playback.currentVolume * 0.390625)); //Set encoder to value
    displayVolume();
  }

}
void showStartupMessage() {
  lcd.clear();
  lcd.setCursor(1, 1); lcd.print("Run the Data Relay");
  lcd.setCursor(1, 2); lcd.print("program on your PC");
}



void loop() {
  //UPDATE METADATA VALUES

  int prevEncoderVal;

  //NEXT
  if (not digitalRead(PIN_MODIFIER) == LOW) {                          //If the modifier button is not pressed
    if ((digitalRead(PIN_NEXT) == LOW) && (oldState_NEXT == false)) {  //If button is pressed but previously not,
      sendCommand("NEXT");                                             //Send message over serial
      oldState_NEXT = true;                                            //Set the old state to true, this was pressed
    } else if (digitalRead(PIN_NEXT) == HIGH) {
      oldState_NEXT = false; //If not pressed, set the old state to false
    }
  } else if ((digitalRead(PIN_NEXT) == LOW) && (oldState_NEXT == false)) {
    backlightLevel = constrain(backlightLevel + 1, 0, 8); //Increase the backlight level
    analogWrite(PIN_BACKLIGHT, round(backlightLevel * BACKLIGHT_STEP));                          //Write the backlight level
    oldState_NEXT = true;
  } else if (digitalRead(PIN_NEXT) == HIGH) {
    oldState_NEXT = false;
  }

  //PREV
  if (not digitalRead(PIN_MODIFIER) == LOW) {                          //If the modifier button is not pressed
    if ((digitalRead(PIN_PREV) == LOW) && (oldState_PREV == false)) {  //If button is pressed but previously not,
      sendCommand("PREV");                                             //Send message over serial
      oldState_PREV = true;                                            //Set the old state to true, this was pressed
    } else if (digitalRead(PIN_PREV) == HIGH) {
      oldState_PREV = false; //If not pressed, set the old state to false
    }
  } else if ((digitalRead(PIN_PREV) == LOW) && (oldState_PREV == false)) {
    backlightLevel = constrain(backlightLevel - 1, 0, 8); //Decrease the backlight level
    analogWrite(PIN_BACKLIGHT, round(backlightLevel * BACKLIGHT_STEP));                          //Write the backlight level
    oldState_PREV = true;
  } else if (digitalRead(PIN_PREV) == HIGH) {
    oldState_PREV = false;
  }

  //SHUF
  if ((digitalRead(PIN_SHUF) == LOW) && (oldState_SHUF == false)) {  //If button is pressed but previously not,
    sendCommand("SHUF");                                             //Send message over serial
    oldState_SHUF = true;                                            //Set the old state to true, this was pressed
  } else if (digitalRead(PIN_SHUF) == HIGH) {
    oldState_SHUF = false; //If not pressed, set the old state to false
  }

  //REPT
  if ((digitalRead(PIN_REPT) == LOW) && (oldState_REPT == false)) {  //If button is pressed but previously not,
    sendCommand("REPT");                                             //Send message over serial
    oldState_REPT = true;                                            //Set the old state to true, this was pressed
  } else if (digitalRead(PIN_REPT) == HIGH) {
    oldState_REPT = false; //If not pressed, set the old state to false
  }

  //PAUS
  if ((digitalRead(PIN_PAUS) == LOW) && (oldState_PAUS == false)) {  //If button is pressed but previously not,
    sendCommand("PAUS");                                             //Send message over serial
    oldState_PAUS = true;                                            //Set the old state to true, this was pressed
  } else if (digitalRead(PIN_PAUS) == HIGH) {
    oldState_PAUS = false; //If not pressed, set the old state to false
  }

  //VOLUME
  //Constrain the value on the encoder to be between 0 and 125
  encoder.write(constrain(encoder.read(), LOWERBOUND, UPPERBOUND));
  int currentVal =  encoder.read();                       //Read encoder value into variable

  currentVal = round(encoder.read() * 2.56);
  if (currentVal != playback.currentVolume) {             //If encoder has different value to the current volume
    sendCommand(String("VOLUME " + String(currentVal)));  //Set volume to be the encoder value
    displayVolume();                                      //Display the volume
    //------
    playback.currentVolume = currentVal;                  //Update the current value
    //------
  }


}

void displayMetadata() {
  lcd.setCursor(0, 0); lcd.print("                    "); //Clear line
  lcd.setCursor(0, 0); lcd.print(formatForDisplay(metadata.title)); //Print formatted title

  lcd.setCursor(0, 1); lcd.print("                    ");
  lcd.setCursor(0, 1); lcd.print(formatForDisplay(metadata.artist));
}

void displayTime() {
  lcd.setCursor( 0, 3); lcd.print("     "); //Temp, clear a bit of space
  lcd.setCursor( 0, 3); lcd.print(secondsToTime(playback.currentTime));

  String lengthStr = secondsToTime(metadata.timeLength); //Stores track length as string
  lcd.setCursor(20 - lengthStr.length(), 3); //Sets cursor position
  for (int i = 0; i <= lengthStr.length() - 1; i++) { //Clears appropriate amount of space
    lcd.print(" "); //Clear space
  }
  lcd.setCursor(20 - lengthStr.length(), 3); lcd.print(lengthStr);      //Display the length

  //DISPLAY BAR
  //Find available space
  int lengthOfCurrentTime = secondsToTime(playback.currentTime).length();
  int barLength = 20 - lengthOfCurrentTime - secondsToTime(metadata.timeLength).length(); //Find length of bar
  float progress = (float)playback.currentTime / (float)metadata.timeLength; //Get progress through track
  lcd.setCursor(lengthOfCurrentTime, 3);
  for (int i = 0; i <= barLength - 1; i++) {
    lcd.print("-"); //Display bar body
  }
  
  //Plop + on bar
  int pos = int(lengthOfCurrentTime + int(trunc(barLength * progress))); //For some reason constrain doesn't work properly, use own function
  lcd.setCursor(constrain(pos, lengthOfCurrentTime, lengthOfCurrentTime + barLength), 3); lcd.print("+"); //Plop

}

void displayRepeatShuffle() {

  lcd.setCursor(8, 2); lcd.print("           ");                    //Clear line
  if ((playback.shuffle == true) or (playback.repeatMode != 0)) {   //If playback mode not default
    if (playback.shuffle == true) {                                 //If shuffling
      lcd.setCursor(13, 2); lcd.print("\x2\x3");                    //Show shuffle icon
    }
    if (playback.repeatMode == 1) {                                 //If repeat-all
      lcd.setCursor(10, 2);
      lcd.print("\x4\x5");
    } else if (playback.repeatMode == 2) {                          //If repeat-one
      lcd.setCursor(10, 2);
      lcd.print("\x4\x6");
    }
  }

}

void displayVolume() {
  if (isConnected) {
    lcd.setCursor(2, 2); lcd.print("    "); //Clear line
    int currentVal = encoder.read();

    //Constrain values
    if (currentVal < 0) {
      currentVal = 0;
    }
    if (currentVal > 125) {
      currentVal = 125;
    }

    //Print icon
    lcd.setCursor(0, 2); lcd.print("\x7\x8 ");
    lcd.print(currentVal); //Print volume on line 3
  }
}

String secondsToTime(int seconds) {
  //Convert time in seconds to string with separators
  bool displayHours = true;
  String secs = String(seconds % 60); //No of seconds
  String mins = String((seconds / 60) % 60); //No of minutes
  String hours = String((seconds / 3600) % 60); //No of hours

  //If hours is unnecessary, do not display them
  if (hours == "0") {
    displayHours = false;
  }

  //Format to be two digits
  if (secs.length() == 1) {
    secs = String("0" + secs);
  }
  if (mins.length() == 1) {
    mins = String("0" + mins);
  }

  if (displayHours) { //If hours is to be displayed
    return String(hours + ":" + mins + ":" + secs);
  } else {
    return String(mins + ":" + secs);
  }

}

String formatForDisplay(String raw) {
  if (raw.length() <= 20) {                      //If string can be displayed without shortening
    return raw;                                  //Just return the string
  } else {                                       //If needs shortening
    return String(raw.substring(0, 19) + "\x1"); //Returns first nineteen characters followed by custom char 1 (ellipsis)
  }
}

void sendCommand(String command) {
  //Code to send commands pause, next, previous, shuffle, repeat, volumeUp, volumeDown
  Serial.println(command);
}
