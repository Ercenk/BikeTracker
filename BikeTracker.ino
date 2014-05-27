// Modified from Adafruit's 10DOF and GPS libraries

// DOF libraries
#include <Wire.h>
#include "Time.h"
#include <Adafruit_Sensor.h>
#include <Adafruit_L3GD20_U.h>
#include <Adafruit_LSM303_U.h>
#include <Adafruit_BMP085_U.h>
#include <Adafruit_10DOF.h>

// GPS library
#include <Adafruit_GPS.h>

#ifdef __AVR__
  #include <SoftwareSerial.h>
  #include <avr/sleep.h>
#endif

#include <SPI.h>
#include <SD.h>

#define LOGSERIAL true
// GPS SETUP SECTION
// ____________________________________________________________________

// Ladyada's logger modified by Bill Greiman to use the SdFat library
//
// This code shows how to listen to the GPS module in an interrupt
// which allows the program to have more 'freedom' - just parse
// when a new NMEA sentence is available! Then access data when
// desired.
//
// Tested and works great with the Adafruit Ultimate GPS Shield
// using MTK33x9 chipset
//    ------> http://www.adafruit.com/products/
// Pick one up today at the Adafruit electronics shop 
// and help support open source hardware & software! -ada

#define mySerial Serial1

Adafruit_GPS GPS(&mySerial);

// Set GPSECHO to 'false' to turn off echoing the GPS data to the Serial console
// Set to 'true' if you want to debug and listen to the raw GPS sentences
#define GPSECHO  true
/* set to true to only log to SD when GPS has a fix, for debugging, keep it false */
#define LOG_FIXONLY false  

// Set the pins used
#define chipSelect 10
#define ledPin 13

File logfile;

// blink out an error code
void error(uint8_t errno) {
/*
  if (SD.errorCode()) {
    putstring("SD error: ");
    myPrint(card.errorCode(), HEX);
    myPrint(',');
    myPrintln(card.errorData(), HEX);
  }
  */
  while(1) {
    uint8_t i;
    for (i=0; i<errno; i++) {
      digitalWrite(ledPin, HIGH);
      delay(100);
      digitalWrite(ledPin, LOW);
      delay(100);
    }
    for (i=errno; i<10; i++) {
      delay(200);
    }
  }
}

void myPrintln(char *str) {
  Serial.println(str);
}

void myPrint(char *str) {
  Serial.print(str);
}

void setup_gps(){
  
  // see if the card is present and can be initialized:
  if (!SD.begin(chipSelect, 11, 12, 13)) {
  //if (!SD.begin(chipSelect)) {      // if you're using an UNO, you can use this line instead
    myPrintln("Card init. failed!");
    error(2);
  }
  char filename[13];
  strcpy(filename, "LOG000.TXT");
  for (uint8_t i = 0; i < 1000; i++) {
    filename[3] = '0' + i/100;
    filename[4] = '0' + (i%100)/10;
     filename[5] = '0' + (i%100)%10;
    // create if does not exist, do not open existing, write, sync after write
    if (! SD.exists(filename)) {
      break;
    }
  }

  logfile = SD.open(filename, FILE_WRITE);
  if( ! logfile ) {
    myPrint("Couldnt create "); myPrintln(filename);
    error(3);
  }
  myPrint("Writing to "); myPrintln(filename);
  
  // connect to the GPS at the desired rate
  GPS.begin(9600);

  // uncomment this line to turn on RMC (recommended minimum) and GGA (fix data) including altitude
  GPS.sendCommand(PMTK_SET_NMEA_OUTPUT_RMCGGA);
  // uncomment this line to turn on only the "minimum recommended" data
  //GPS.sendCommand(PMTK_SET_NMEA_OUTPUT_RMCONLY);
  // For logging data, we don't suggest using anything but either RMC only or RMC+GGA
  // to keep the log files at a reasonable size
  // Set the update rate
  GPS.sendCommand(PMTK_SET_NMEA_UPDATE_5HZ);   // 1 or 5 Hz update rate

  // Turn off updates on antenna status, if the firmware permits it
  GPS.sendCommand(PGCMD_NOANTENNA);
}


// 10 DOF setup section
// -------------------------------------------

/* Assign a unique ID to the sensors */
Adafruit_10DOF                dof   = Adafruit_10DOF();
Adafruit_LSM303_Accel_Unified accel = Adafruit_LSM303_Accel_Unified(30301);
Adafruit_LSM303_Mag_Unified   mag   = Adafruit_LSM303_Mag_Unified(30302);
Adafruit_BMP085_Unified       bmp   = Adafruit_BMP085_Unified(18001);
Adafruit_L3GD20_Unified       gyro = Adafruit_L3GD20_Unified(20);

/* Update this with the correct SLP for accurate altitude measurements */
float seaLevelPressure = SENSORS_PRESSURE_SEALEVELHPA;

/**************************************************************************/
/*!
    @brief  Initialises all the sensors used by this example
*/
/**************************************************************************/
void initSensors()
{
    /* Enable auto-ranging */
  gyro.enableAutoRange(true);
  
  /* Initialise the sensor */
  if(!gyro.begin())
  {
    /* There was a problem detecting the L3GD20 ... check your connections */
    myPrintln("Ooops, no L3GD20 detected ... Check your wiring!");
    while(1);
  }
  
  if(!accel.begin())
  {
    /* There was a problem detecting the LSM303 ... check your connections */
    myPrintln("Ooops, no LSM303 detected ... Check your wiring!");
    while(1);
  }
  if(!mag.begin())
  {
    /* There was a problem detecting the LSM303 ... check your connections */
    myPrintln("Ooops, no LSM303 detected ... Check your wiring!");
    while(1);
  }
  if(!bmp.begin())
  {
    /* There was a problem detecting the BMP180 ... check your connections */
    myPrintln("Ooops, no BMP180 detected ... Check your wiring!");
    while(1);
  }
}

size_t trimwhitespace(char *out, size_t len, const char *str)
{
  if(len == 0)
    return 0;

  const char *end;
  size_t out_size;

  // Trim leading space
  while(isspace(*str)) str++;

  if(*str == 0)  // All spaces?
  {
    *out = 0;
    return 1;
  }

  // Trim trailing space
  end = str + strlen(str) - 1;
  while(end > str && isspace(*end)) end--;
  end++;

  // Set output size to minimum of trimmed string length and buffer size minus 1
  out_size = (end - str) < len-1 ? (end - str) : len-1;

  // Copy trimmed string and add null terminator
  memcpy(out, str, out_size);
  out[out_size] = 0;

  return out_size;
}

void setup() {
  // for Leonardos, if you want to debug SD issues, uncomment this line
  // to see serial output
  //while (!Serial);
  
  // connect at 115200 so we can read the GPS fast enough and echo without dropping chars
  // also spit it out
  Serial.begin(115200);
  
  pinMode(ledPin, OUTPUT);

  // make sure that the default chip select pin is set to
  // output, even if you don't use it:
  pinMode(10, OUTPUT);
  
  setup_gps();
  initSensors();
  
  myPrintln("gps setup complete");
}

void loop() {

  char logBuffer[256];
  char gpsLogBuffer[128];
  size_t gpsBufferSize = 0;

  // GPS section
  char c = GPS.read();

  // if a sentence is received, we can check the checksum, parse it...
  if (GPS.newNMEAreceived()) {
    // a tricky thing here is if we print the NMEA sentence, or data
    // we end up not listening and catching other sentences! 
    // so be very wary if using OUTPUT_ALLDATA and trying to print out data
    //myPrintln(GPS.lastNMEA());   // this also sets the newNMEAreceived() flag to false
        
    char *stringptr = GPS.lastNMEA();    
    if (GPS.parse(stringptr))   
    {
        if (GPSECHO)
           Serial.println(stringptr);
           
      if (!GPS.fix) {
        sprintf(gpsLogBuffer, "NO GPS");
      }
      else {
        sprintf(gpsLogBuffer, "%d/%d/%d,%d:%d:%d.%d,%d,%d,%f,%f,%f,%f,%f,%d,%f,%f,", 
          GPS.month, GPS.day, GPS.year, GPS.hour, GPS.minute, GPS.seconds, GPS.milliseconds, GPS.fix, GPS.fixquality,
          GPS.latitude, GPS.longitude, GPS.speed, GPS.angle, GPS.altitude, GPS.satellites, GPS.HDOP, GPS.magvariation);
      }

      gpsBufferSize = trimwhitespace(logBuffer, 128, gpsLogBuffer);
    }
  
  if (gpsBufferSize == 0)
  {
      return;
  }
  
  if (LOG_FIXONLY && !GPS.fix) {
        Serial.print("No Fix");
    }
    
    // DOF section
  sensors_event_t accel_event;
  sensors_event_t mag_event;
  sensors_event_t bmp_event;
  sensors_vec_t   orientation;

  char dofLogBuffer[128];
  float roll, pitch, heading, compHeading, temperature, altitude;

  /* Calculate pitch and roll from the raw accelerometer data */
  accel.getEvent(&accel_event);
  if (dof.accelGetOrientation(&accel_event, &orientation))
  {
    roll = orientation.roll;
    pitch = orientation.pitch;    
  }
  
  /* Calculate the heading using the magnetometer */
  mag.getEvent(&mag_event);
  if (dof.magGetOrientation(SENSOR_AXIS_Z, &mag_event, &orientation))
  {
    heading = orientation.heading;
  }
  
  if (dof.magTiltCompensation(SENSOR_AXIS_Z, &mag_event, &accel_event))
  {
     if (dof.magGetOrientation(SENSOR_AXIS_Z, &mag_event, &orientation))
    {
      compHeading = orientation.heading;
    }
  }

  /* Calculate the altitude using the barometric pressure sensor */
  bmp.getEvent(&bmp_event);
  if (bmp_event.pressure)
  {
    /* Get ambient temperature in C */
   
    bmp.getTemperature(&temperature);
    //altitude = bmp.pressureToAltitude(seaLevelPressure,
    altitude = bmp.pressureToAltitude(1021,
                                        bmp_event.pressure,
                                        temperature); 
  }

  sprintf(dofLogBuffer, "%f,%f,%f,%f,%f,%f\n", roll, pitch, heading, compHeading, temperature, altitude);
  gpsBufferSize = trimwhitespace(logBuffer + gpsBufferSize, 128, dofLogBuffer);

  if (LOGSERIAL) {
    Serial.println(logBuffer);
  }
  
  char newLine[2] = "\n";  
  uint8_t stringsize = strlen(logBuffer);
  if (stringsize != logfile.write((uint8_t *)logBuffer, stringsize))    //write the string to the SD file
    error(4);
  else logfile.write((uint8_t *)newLine, 1);
  
   delay(2000);
   logfile.flush();
  }
}


/* End code */
