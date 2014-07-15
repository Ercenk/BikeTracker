// Common
#include <Wire.h>
#include <SPI.h>
#define LOGSERIAL true

// PINS
#define DO_NOT_USE_TX 7 // GPS TX
#define DO_NOT_USE_RX 8 // GPS RX
#define gpsSerial Serial1

#define SD_ChipSelect 10
#define SD_MOSI 11 // Selected b defult by the SDFat library
#define SD_MISO 12 // Selected b defult by the SDFat library
#define SD_SCK 13 // Selected b defult by the SDFat library

#define OLED_DC     6
#define OLED_CS     4
#define OLED_RST  5

#define SetupSerialLog() if (LOGSERIAL) Serial.begin(115200)

// GPS
#include <TinyGPS++.h>
#define PMTK_SET_NMEA_UPDATE_1HZ  F("$PMTK220,1000*1F")
#define PMTK_SET_NMEA_UPDATE_5HZ  F("$PMTK220,200*2C")
#define PMTK_SET_NMEA_UPDATE_10HZ F("$PMTK220,100*2F")


#define PMTK_SET_BAUD_57600 F("$PMTK251,57600*2C")
#define PMTK_SET_BAUD_9600 F("$PMTK251,9600*17")

// turn on only the second sentence (GPRMC)
#define PMTK_SET_NMEA_OUTPUT_RMCONLY F("$PMTK314,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0*29")
// turn on GPRMC and GGA
#define PMTK_SET_NMEA_OUTPUT_RMCGGA F("$PMTK314,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0*28")
// turn on ALL THE DATA
#define PMTK_SET_NMEA_OUTPUT_ALLDATA F("$PMTK314,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0*28")
// turn off output
#define PMTK_SET_NMEA_OUTPUT_OFF F("$PMTK314,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0*28")

#define PMTK_LOCUS_STARTLOG  F("$PMTK185,0*22")
#define PMTK_LOCUS_LOGSTARTED F("$PMTK001,185,3*3C")
#define PMTK_LOCUS_QUERY_STATUS F("$PMTK183*38")
#define PMTK_LOCUS_ERASE_FLASH F("$PMTK184,1*22")
#define LOCUS_OVERLAP 0
#define LOCUS_FULLSTOP 1

// standby command & boot successful message
#define PMTK_STANDBY F("$PMTK161,0*28")
#define PMTK_STANDBY_SUCCESS F("$PMTK001,161,3*36")  // Not needed currently
#define PMTK_AWAKE F("$PMTK010,002*2D")

// ask for the release and version
#define PMTK_Q_RELEASE F("$PMTK605*31")

// request for updates on antenna status
#define PGCMD_ANTENNA F("$PGCMD,33,1*6C")
#define PGCMD_NOANTENNA F("$PGCMD,33,0*6C")

#define GPSACCURACYFACTOR 7.8

TinyGPSPlus gps;

// SD
#include <SdFat.h>
SdFat sd;
// text file for logging
ofstream logfile;

// OLED
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
Adafruit_SSD1306              display = Adafruit_SSD1306(OLED_DC, OLED_RST, OLED_CS);

// Serial print stream
ArduinoOutStream serialLog(Serial);

// store error strings in flash to save RAM
#define error(s) sd.errorHalt_P(PSTR(s))


uint32_t timer, logTimer;
bool timeToLog = false,
     sdError = true,
     logFileIsOpen = false,
     moving = false;
double currentSpeed, maxSpeed = 0, avgSpeed = 0;
uint32_t dataCounter = 0;

double prevLat = 0, prevLng = 0, distance = 0, lat, lng;

uint16_t year;
byte month, day, hour, minutes, second, hundredths;

char logBuffer[256];

void printAt(int16_t x, int16_t y, uint8_t s, char *str)
{
  display.setTextSize(s);
  display.setTextColor(WHITE);
  display.setCursor(x, y);
  display.println(str);
  display.display();
}

void setup() {
  // Common setup
  SetupSerialLog();

  // GPS setup
  gpsSerial.begin(9600);
  gpsSerial.println(PMTK_SET_NMEA_OUTPUT_ALLDATA);
  gpsSerial.println(PMTK_SET_NMEA_UPDATE_5HZ);
  gpsSerial.println(PGCMD_ANTENNA);

  // SD setup
  if (sd.begin(SD_ChipSelect, SPI_HALF_SPEED)) sdError = false;

  char filename[13];
  strcpy(filename, "LOG000.TXT");
  for (uint8_t i = 0; i < 1000; i++) {
    filename[3] = '0' + i / 100;
    filename[4] = '0' + (i % 100) / 10;
    filename[5] = '0' + (i % 100) % 10;
    if (!sd.exists(filename)) {
      break;
    }
  }

  logfile.open(filename);
  if (logfile.is_open()) logFileIsOpen = true;

  if (LOGSERIAL) {
    Serial.print("logging to: "); Serial.println(filename);
  }

  display.begin(SSD1306_SWITCHCAPVCC);
  display.clearDisplay();
  //Size 2 shows 30 characters, the next ones are cut.
  // Size 1 shows 147 chars

  char displayBuffer[50];
  char floatBuffer[6];

  dtostrf((double) (sd.vol()->freeClusterCount() * sd.vol()->blocksPerCluster() / 2) / 1024 / 1024,
          5, 2, floatBuffer);
  sprintf(displayBuffer, "%s, %s", filename, floatBuffer);
  printAt(0, 8, 1, displayBuffer);

  delay(1000);
}


void loop() {
  if (timer > millis())  timer = millis();
  if (logTimer > millis())  logTimer = millis();

  // Get data only every other second
  if ((millis() - timer) <= 2000) {
    return;
  }

  // Reset the timer
  timer = millis();

  if (millis() - logTimer > 60000) {
    timeToLog = true;
  }

  // define the output buffer
  obufstream outBuffer(logBuffer, sizeof(logBuffer));

  for (unsigned long start = millis(); millis() - start < 1000;)
  {
    while (gpsSerial.available())
    {
      char c = gpsSerial.read();
      gps.encode(c);
    }
  }

  if (gps.location.isUpdated() && gps.location.isValid())
  {
    lat = gps.location.lat();
    lng = gps.location.lng();

    if (prevLng == 0) {
      prevLng = lng;
      prevLat = lat;
    }
    distance = gps.distanceBetween(lat, lng, prevLat, prevLng);
    if (distance > GPSACCURACYFACTOR) {
      moving = true;
    }

    if (timeToLog) {
      if (!moving) {
        timeToLog = false;
      }
      moving = false;
    }

    prevLng = lng;
    prevLat = lat;

    dataCounter++;
    currentSpeed = gps.speed.mph();
    if (currentSpeed > maxSpeed) {
      maxSpeed = currentSpeed;
    }
    avgSpeed = ((avgSpeed * (dataCounter - 1)) + currentSpeed) / dataCounter;

    char oldFill = outBuffer.fill('0');
    outBuffer <<
              setw(2) << int(gps.date.year()) << '-' <<
              setw(2) << int(gps.date.month()) << '-' <<
              setw(2) << int(gps.date.day()) << 'T' <<
              setw(2) << int(gps.time.hour()) << ':' <<
              setw(2) << int(gps.time.minute()) << ':' <<
              setw(2) << int(gps.time.second()) << 'Z' ;
    outBuffer.fill(oldFill);

    outBuffer << ',' <<
              //dateTimeBuffer << ',' <<
              setprecision(10) << lat << ',' <<
              setprecision(10) << lng << ',' <<
              setprecision(1) << currentSpeed  << ',' <<
              setprecision(1) << maxSpeed  << ',' <<
              setprecision(1) << avgSpeed  << ',' <<
              setprecision(2) << gps.course.deg() << ',' <<
              setprecision(0) << gps.altitude.feet() << ',' <<
              gps.satellites.value() << endl;

    double sdSize = (double) (sd.vol()->freeClusterCount() * sd.vol()->blocksPerCluster() / 2) / 1024 / 1024;
    if (LOGSERIAL) {
      serialLog << logBuffer;
    }
  }

  if (timeToLog) {
    logfile << logBuffer << flush;
  }
}
