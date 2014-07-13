// Common
#include <SPI.h>
#include <Wire.h>
#include "Time.h"
#include "string.h"

// SD
#include <SD.h>

// GPS
#include <Adafruit_GPS.h>

// 10DOF
#include <Adafruit_Sensor.h>
#include <Adafruit_L3GD20_U.h>
#include <Adafruit_LSM303_U.h>
#include <Adafruit_BMP085_U.h>
#include <Adafruit_10DOF.h>

// OLED
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define LOGSERIAL true
#define MAXSTRINGLENGTH 256

/* Update this with the correct SLP for accurate altitude measurements */
float seaLevelPressure = SENSORS_PRESSURE_SEALEVELHPA;
#define EARTHRADIUS 637100.9
#define ONEDEGREEINRADIANS 0.01745329
#define MILESINNAUTICALMILES 1.15077945
#define GPSACCURACYFACTOR 7.8

// GPS & SD
#define gpsSerial Serial1
#define TX 8 // not used
#define RX 7 // not used
#define SD_ChipSelect 10
#define SD_MOSI 11
#define SD_MISO 12
#define SD_SCK 13
#define ledPin 13

#define OLED_DC     6
#define OLED_CS     4
#define OLED_RESET  5

#define BLE_REQ  3
#define BLE_RDY  24
#define BLE_RST  9

char gpsNoDataString[] = "0/0/0,0:0:0.0,0,0,0,0,0,0,0,0,0,0,";

Adafruit_GPS GPS(&gpsSerial);
// 10DOF
/*
Adafruit_10DOF                dof   = Adafruit_10DOF();
Adafruit_LSM303_Accel_Unified accel = Adafruit_LSM303_Accel_Unified(30301);
Adafruit_LSM303_Mag_Unified   mag   = Adafruit_LSM303_Mag_Unified(30302);
Adafruit_BMP085_Unified       bmp   = Adafruit_BMP085_Unified(18001);
Adafruit_L3GD20_Unified       gyro = Adafruit_L3GD20_Unified(20);
*/
Adafruit_SSD1306              display = Adafruit_SSD1306(OLED_DC, OLED_RESET, OLED_CS);

File logfile;

char displayBuffer[20];

void error(uint8_t errno)
{
  while (1) {
    uint8_t i;
    for (i = 0; i < errno; i++) {
      digitalWrite(ledPin, HIGH);
      delay(100);
      digitalWrite(ledPin, LOW);
      delay(100);
    }
    for (i = errno; i < 10; i++) {
      delay(200);
    }
  }
}

size_t trimwhitespace(char *out, size_t len, const char *str)
{
  if (len == 0)
    return 0;

  const char *end;
  size_t out_size;

  // Trim leading space
  while (isspace(*str)) str++;

  if (*str == 0) // All spaces?
  {
    *out = 0;
    return 1;
  }

  // Trim trailing space
  end = str + strlen(str) - 1;
  while (end > str && isspace(*end)) end--;
  end++;

  // Set output size to minimum of trimmed string length and buffer size minus 1
  out_size = (end - str) < len - 1 ? (end - str) : len - 1;

  // Copy trimmed string and add null terminator
  memcpy(out, str, out_size);
  out[out_size] = 0;

  return out_size;
}

void setupGps()
{
  GPS.begin(9600);
  gpsSerial.begin(9600);

  GPS.sendCommand(PMTK_SET_NMEA_OUTPUT_RMCGGA);
  //GPS.sendCommand(PMTK_SET_NMEA_OUTPUT_RMCONLY);
  GPS.sendCommand(PMTK_SET_NMEA_UPDATE_1HZ);   // 1 Hz update rate
  GPS.sendCommand(PGCMD_ANTENNA);

  delay(1000);
  gpsSerial.println(PMTK_Q_RELEASE);
}

/*
void setupSensors()
{
  gyro.enableAutoRange(true);

  if (!gyro.begin())
  {
    if (LOGSERIAL) {
      Serial.println("No L3GD20 detected.");
    }
    while (1);
  }

  if (!accel.begin())
  {
    if (LOGSERIAL) {
      Serial.println("No LSM303 detected.");
    }
    while (1);
  }
  if (!mag.begin())
  {
    if (LOGSERIAL) {
      Serial.println("No LSM303 detected.");
    }
    while (1);
  }
  if (!bmp.begin())
  {
    if (LOGSERIAL) {
      Serial.println("No BMP180 detected.");
    }
    while (1);
  }
}
*/

void setupSD()
{
  if (!SD.begin(SD_ChipSelect, SD_MOSI, SD_MISO, SD_SCK)) {
    if (LOGSERIAL) {
      Serial.println("Card init. failed!");
    }
    error(2);
  }

  char filename[13];
  strcpy(filename, "LOG000.TXT");
  for (uint8_t i = 0; i < 1000; i++) {
    filename[3] = '0' + i / 100;
    filename[4] = '0' + (i % 100) / 10;
    filename[5] = '0' + (i % 100) % 10;
    if (! SD.exists(filename)) {
      break;
    }
  }

  logfile = SD.open(filename, FILE_WRITE);

  if ( ! logfile ) {
    if (LOGSERIAL) {
      Serial.println("Couldnt create ");
    }
    error(3);
    Serial.print(filename);
  }

  if (LOGSERIAL) {
    Serial.println("Writing to ");
    Serial.print(filename);
  }
}

void printAt(int16_t x, int16_t y, uint8_t s, char *str)
{
  display.setTextSize(s);
  display.setTextColor(WHITE);
  display.setCursor(x, y);
  display.println(str);
  display.display();
}

void setupDisplay()
{
  display.begin(SSD1306_SWITCHCAPVCC);
  display.clearDisplay();
  printAt(0, 8, 2, "ERCENK");
}
void getSensorData(char *dofLogBuffer)
{



  /*
    display.clearDisplay();
    sprintf(displayBuffer, "X:%f Y:%f Z:%f", accel_event.acceleration.x, accel_event.acceleration.y, accel_event.acceleration.z);
    printAt(0, 0, 2, displayBuffer);

    Serial.println(displayBuffer);
    */
}

void setup()
{
  Serial.begin(115200);
  setupSD();
  setupGps();
  //setupSensors();
  setupDisplay();
}

float getDecimalDegree(float nmeaValue) {
  float minutes;
  float degrees;
  float seconds;
  float milliseconds;

  degrees = trunc(nmeaValue / 100);
  minutes = nmeaValue - (degrees * 100);
  seconds = (minutes - trunc(minutes)) * 60;
  milliseconds = (seconds - trunc(seconds)) * 1000;

  minutes = trunc(minutes);
  seconds = trunc(seconds);

  return degrees + minutes / 60 + seconds / 3600 + milliseconds / 3600000;
}

uint32_t timer = millis();
float previousLatitude = 0, previousLongitude = 0;

char logBuffer[256];
char gpsLogBuffer[128];
char dofLogBuffer[128];
size_t gpsBufferSize = 0;
bool shouldLog = false;
float distance;

uint8_t gpsHour;
uint8_t gpsMinute;
uint8_t gpsSeconds;
uint8_t gpsYear;
uint8_t gpsMonth;
uint8_t gpsDay;
uint16_t gpsMilliseconds;
float latitude;
float longitude;
float geoidheight;
float gpsaltitude;
float gpsSpeed;
float angle;
float magvariation;
float HDOP;
char lat;
char lon;
char gpsMag;
boolean fix;
uint8_t fixquality;
uint8_t satellites;

// DOF
sensors_event_t accel_event;
sensors_event_t mag_event;
sensors_event_t bmp_event;
sensors_event_t gyro_event;
sensors_vec_t   orientation;

float roll, pitch, heading, compHeading, temperature, dofAltitude;

void loop()
{
  char c = GPS.read();

  // if a sentence is received, we can check the checksum, parse it...
  if (GPS.newNMEAreceived()) {
    // a tricky thing here is if we print the NMEA sentence, or data
    // we end up not listening and catching other sentences!
    // so be very wary if using OUTPUT_ALLDATA and trytng to print out data
    //Serial.println(GPS.lastNMEA());   // this also sets the newNMEAreceived() flag to false

    if (!GPS.parse(GPS.lastNMEA()))   // this also sets the newNMEAreceived() flag to false
      return;  // we can fail to parse a sentence in which case we should just wait for another
  }

  // Copy GPS data first
  gpsHour = GPS.hour;
  gpsMinute = GPS.minute;
  gpsSeconds = GPS.seconds;
  gpsYear = GPS.year;
  gpsMonth = GPS.month;
  gpsDay = GPS.day;
  gpsMilliseconds = GPS.milliseconds;
  latitude = GPS.latitude;
  latitude = getDecimalDegree(latitude);
  longitude = GPS.longitude;
  geoidheight = GPS.geoidheight;
  gpsaltitude = GPS.altitude;
  gpsSpeed = GPS.speed;
  gpsSpeed *= MILESINNAUTICALMILES;
  angle = GPS.angle;
  magvariation = GPS.magvariation;
  HDOP = GPS.HDOP;
  lat = GPS.lat;
  if (lat == 'S')
    latitude *= -1;
  lon = GPS.lon;
  if (lon == 'W')
    longitude *= -1;

  gpsMag = GPS.mag;
  fix = GPS.fix;
  fixquality = GPS.fixquality;
  satellites = GPS.satellites;
  /*
  
  // DOF
  accel.getEvent(&accel_event);
  if (dof.accelGetOrientation(&accel_event, &orientation))
  {
    roll = orientation.roll;
    pitch = orientation.pitch;
  }

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

  bmp.getEvent(&bmp_event);
  if (bmp_event.pressure)
  {
    bmp.getTemperature(&temperature);
    //altitude = bmp.pressureToAltitude(seaLevelPressure,
    dofAltitude = bmp.pressureToAltitude(1021,
                                      bmp_event.pressure,
                                      temperature);
  }

  gyro.getEvent(&gyro_event);
  sprintf(dofLogBuffer, "%f,%f,%f,%f,%f,%f,%f,%f,%f,%f,%f,%f", roll, pitch, heading, compHeading, temperature, dofAltitude, accel_event.acceleration.x, accel_event.acceleration.y, accel_event.acceleration.z, gyro_event.gyro.x, gyro_event.gyro.y, gyro_event.gyro.z);
  Serial.println(dofLogBuffer);
  */
  
  if (timer > millis())  timer = millis();

  if ((millis() - timer) > 2000)
  {
    // Are we moving?
    float deltaLat = latitude - previousLatitude;
    float deltaLong = longitude - previousLongitude;
    float meanLatRadians = ((latitude * ONEDEGREEINRADIANS) + (previousLatitude * ONEDEGREEINRADIANS)) / 2;

    previousLatitude = latitude;
    previousLongitude = longitude;

    distance = EARTHRADIUS * sqrt(pow(deltaLat, 2) + pow(cos(meanLatRadians) * deltaLong, 2));
    shouldLog = distance > GPSACCURACYFACTOR;

    timer = millis(); // reset the timer
  }

  if (!fix) {
    if (shouldLog)
    {
      sprintf(gpsLogBuffer, gpsNoDataString);
      if (LOGSERIAL) {
        Serial.println("no fix");
      }
    }
    else
      return;
  }
  else {
    if (shouldLog) {
      sprintf(gpsLogBuffer, "%f, %d/%d/%d,%d:%d:%d.%d,%d,%d,%f,%f,%f,%f,%f,%d,%f,%f,",
              distance, gpsMonth, gpsDay, gpsYear, gpsHour, gpsMinute, gpsSeconds, gpsMilliseconds, fix, fixquality,
              latitude, longitude, gpsSpeed, angle, gpsaltitude, satellites, HDOP, magvariation);
    }
  }

  if (shouldLog)
  {
    gpsBufferSize = trimwhitespace(logBuffer, 128, gpsLogBuffer);
    gpsBufferSize = trimwhitespace(logBuffer + gpsBufferSize, 128, dofLogBuffer);

    if (LOGSERIAL) {
      Serial.println(logBuffer);
    }

    char newLine[2] = "\n";
    uint8_t stringsize = strlen(logBuffer);
    if (stringsize != logfile.write((uint8_t *)logBuffer, stringsize))
    {
      if (LOGSERIAL) {
        Serial.println("Error writing file");
      }
      error(4);
    }
    else logfile.write((uint8_t *)newLine, 1);
    logfile.flush();
  }
}
