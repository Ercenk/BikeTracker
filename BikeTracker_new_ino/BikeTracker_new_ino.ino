// Common
#include <Wire.h>
#include <SPI.h>
#define LOGSERIAL false

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

#define ASK_RX_PIN 44 // NO RX CAPABILITY BUT TO KEEP CONSTRUCTOR HAPPY
#define ASK_TX_PIN 46
#define ASK_PTTIN_PIN 48

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

#define GPSACCURACYFACTOR 2.5

TinyGPSPlus gps;

// SD
#include <SdFat.h>
SdFat sd;
// text file for logging
ofstream logfile;

// Serial print stream
ArduinoOutStream serialLog(Serial);

// store error strings in flash to save RAM
#define error(s) sd.errorHalt_P(PSTR(s))

// OLED
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
Adafruit_SSD1306              display = Adafruit_SSD1306(OLED_DC, OLED_RST, OLED_CS);

// 10DOF
#include <Adafruit_Sensor.h>
#include <Adafruit_L3GD20_U.h>
#include <Adafruit_LSM303_U.h>
#include <Adafruit_BMP085_U.h>
#include <Adafruit_10DOF.h>

Adafruit_10DOF                dof   = Adafruit_10DOF();
Adafruit_LSM303_Accel_Unified accel = Adafruit_LSM303_Accel_Unified(30301);
Adafruit_LSM303_Mag_Unified   mag   = Adafruit_LSM303_Mag_Unified(30302);
Adafruit_BMP085_Unified       bmp   = Adafruit_BMP085_Unified(18001);
Adafruit_L3GD20_Unified       gyro = Adafruit_L3GD20_Unified(20);

uint32_t logTimer, displayTimer, distanceTimer;
bool timeToLog = false,
     sdError = true,
     logFileIsOpen = false,
     moving = false;
double currentSpeed, maxSpeed = 0, avgSpeed = 0;
uint32_t dataCounter = 0;

double prevLat = 0, prevLng = 0, distance = 0, lat, lng;

uint16_t year;
byte month, day, hour, minutes, second, hundredths;

// DOF
sensors_event_t accel_event;
sensors_event_t mag_event;
sensors_event_t bmp_event;
sensors_event_t gyro_event;
sensors_vec_t   orientation;

float roll, pitch, heading, compHeading, temperature, barometricPressure, bmpAltitude;
float maxRoll, maxPitch, maxTemperature, maxBarometricPressure, maxBmpAltitude;
float avgRoll, avgPitch, avgTemperature, avgBarometricPressure, avgBmpAltitude;

float maxAccelX, maxAccelY, maxAccelZ, avgAccelX, avgAccelY, avgAccelZ;

// Radio
#include <RH_ASK.h>
#include <SPI.h> // Not actually used but needed to compile

RH_ASK radio(2000, ASK_RX_PIN, ASK_TX_PIN, ASK_PTTIN_PIN, false);


char filename[13];
char logBuffer[250];
char gpsLogBuffer[100];
char dofLogBuffer[150];
char displayBuffer[64];
uint8_t recNumber = 0;

uint8_t fileNumber;

size_t gpsBufferSize = 0;

void getMaxAndAvg(float val, float *maxVal, float *avgVal, uint32_t counter) {
  if (abs(val) > abs(*maxVal) || counter == 0) {
    *maxVal = val;
  }

  if (counter == 0)
    *avgVal = val;
  else
    *avgVal = ((*avgVal * (counter - 1)) + val) / counter;
}

void getMaxAndAvg(double val, double *maxVal, double *avgVal, uint32_t counter) {
  if (abs(val) > abs(*maxVal) || counter == 0) {
    *maxVal = val;
  }

  if (counter == 0)
    *avgVal = val;
  else
    *avgVal = ((*avgVal * (counter - 1)) + val) / counter;
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

void radioSend(uint8_t count, char dataSpec, double data)
{
  char txBuffer[RH_ASK_MAX_MESSAGE_LEN];
  sprintf(txBuffer, "%c%s,%d", dataSpec, String(data).c_str(), count);
  radio.send((uint8_t *)txBuffer, strlen(txBuffer));
  radio.waitPacketSent();
}

long readVcc() {
  // Read 1.1V reference against AVcc
  // set the reference to Vcc and the measurement to the internal 1.1V reference
#if defined(__AVR_ATmega32U4__) || defined(__AVR_ATmega1280__) || defined(__AVR_ATmega2560__)
  ADMUX = _BV(REFS0) | _BV(MUX4) | _BV(MUX3) | _BV(MUX2) | _BV(MUX1);
#elif defined (__AVR_ATtiny24__) || defined(__AVR_ATtiny44__) || defined(__AVR_ATtiny84__)
  ADMUX = _BV(MUX5) | _BV(MUX0);
#elif defined (__AVR_ATtiny25__) || defined(__AVR_ATtiny45__) || defined(__AVR_ATtiny85__)
  ADMUX = _BV(MUX3) | _BV(MUX2);
#else
  ADMUX = _BV(REFS0) | _BV(MUX3) | _BV(MUX2) | _BV(MUX1);
#endif

  delay(2); // Wait for Vref to settle
  ADCSRA |= _BV(ADSC); // Start conversion
  while (bit_is_set(ADCSRA, ADSC)); // measuring

  uint8_t low  = ADCL; // must read ADCL first - it then locks ADCH
  uint8_t high = ADCH; // unlocks both

  long result = (high << 8) | low;

  result = 1125300L / result; // Calculate Vcc (in mV); 1125300 = 1.1*1023*1000
  return result; // Vcc in millivolts
}

void displayStatus(char *status)
{
  char displayBuffer[64];

  uint8_t stopWatch = millis();
  display.clearDisplay();
  display.setTextSize(2);
  display.setTextColor(WHITE);
  display.setCursor(0, 0);
  sprintf(displayBuffer, "%d %sGB", fileNumber, logFileIsOpen ? String((double) (sd.vol()->freeClusterCount() * sd.vol()->blocksPerCluster() / 2) / 1024 / 1024, 2).c_str() : 0);
  display.println(displayBuffer);
  sprintf(displayBuffer, "%d mV", readVcc());
  display.println(displayBuffer);
  sprintf(displayBuffer, "%s", status);
  display.println(displayBuffer);
  display.display();
}
void setup() {
  // Common setup
  SetupSerialLog();

  // GPS setup
  gpsSerial.begin(9600);
  gpsSerial.println(PMTK_SET_NMEA_OUTPUT_RMCGGA);
  gpsSerial.println(PMTK_SET_NMEA_UPDATE_5HZ);
  gpsSerial.println(PGCMD_ANTENNA);

  // SD setup
  if (sd.begin(SD_ChipSelect, SPI_HALF_SPEED)) sdError = false;

  strcpy(filename, "LOG000.TXT");
  for (uint8_t i = 0; i < 1000; i++) {
    filename[3] = '0' + i / 100;
    filename[4] = '0' + (i % 100) / 10;
    filename[5] = '0' + (i % 100) % 10;
    if (!sd.exists(filename)) {
      fileNumber = i;
      break;
    }
  }

  logfile.open(filename);
  if (logfile.is_open()) logFileIsOpen = true;

  if (LOGSERIAL) {
    Serial.print("logging to: "); Serial.println(filename);
  }

  display.begin(SSD1306_SWITCHCAPVCC);
  displayStatus("Started");

  if (!accel.begin())
  {
    /* There was a problem detecting the LSM303 ... check your connections */
    Serial.println(F("Ooops, no LSM303 detected ... Check your wiring!"));
    while (1);
  }
  if (!mag.begin())
  {
    /* There was a problem detecting the LSM303 ... check your connections */
    Serial.println("Ooops, no LSM303 detected ... Check your wiring!");
    while (1);
  }
  if (!bmp.begin())
  {
    /* There was a problem detecting the BMP180 ... check your connections */
    Serial.println("Ooops, no BMP180 detected ... Check your wiring!");
    while (1);
  }

  if (!radio.init())
    Serial.println("init failed");

  delay(1000);
}

void loop() {
  while (gpsSerial.available())
    gps.encode(gpsSerial.read());

  // Get pitch roll, heading from DOF
  accel.getEvent(&accel_event);
  mag.getEvent(&mag_event);

  if (dof.fusionGetOrientation(&accel_event, &mag_event, &orientation))
  {
    roll = orientation.roll;
    pitch = orientation.pitch;
    heading = orientation.heading;
  }

  bmp.getEvent(&bmp_event);
  if (bmp_event.pressure)
  {
    bmp.getTemperature(&temperature);
    bmpAltitude = bmp.pressureToAltitude(1021,
                                         bmp_event.pressure,
                                         temperature);
  }

  getMaxAndAvg(roll, &maxRoll, &avgRoll, dataCounter);
  getMaxAndAvg(pitch, &maxPitch, &avgPitch, dataCounter);
  getMaxAndAvg(temperature, &maxTemperature, &avgTemperature, dataCounter);
  getMaxAndAvg(bmp_event.pressure, &maxBarometricPressure, &avgBarometricPressure, dataCounter);
  getMaxAndAvg(bmpAltitude, &maxBmpAltitude, &avgBmpAltitude, dataCounter);
  getMaxAndAvg(accel_event.acceleration.x, &maxAccelX, &avgAccelX, dataCounter);
  getMaxAndAvg(accel_event.acceleration.y, &maxAccelY, &avgAccelY, dataCounter);
  getMaxAndAvg(accel_event.acceleration.z, &maxAccelZ, &avgAccelZ, dataCounter);

  if (logTimer > millis())  logTimer = millis();

  if (millis() - logTimer > 10000) {
    logTimer = millis();
    timeToLog = true;
  }

  bool gpsData = false;
  // gps.location.isUpdated() &&
  if (gps.location.isValid())
  {
    gpsData = true;
    lat = gps.location.lat();
    lng = gps.location.lng();

    if (prevLng == 0) {
      prevLng = lng;
      prevLat = lat;
    }

    currentSpeed = gps.speed.mph();
    getMaxAndAvg(currentSpeed, &maxSpeed, &avgSpeed, dataCounter);
    /*
        if (LOGSERIAL) {
          serialLog << currentSpeed << endl;
        }
        if (distanceTimer > millis())  distanceTimer = millis();
        if (millis() - distanceTimer > 2000) {
          distanceTimer = millis();
          distance = gps.distanceBetween(lat, lng, prevLat, prevLng);
          prevLng = lng;
          prevLat = lat;
          if (distance > (GPSACCURACYFACTOR * 2) && currentSpeed > 3) {
            moving = true;
          }
        }      
        */
    if (currentSpeed > 3) {
      moving = true;
    }

    sprintf(gpsLogBuffer, "%d-%02d-%02dT%02d:%02d:%02dZ,%s,%s,%s,%s,%s,%s,%s,%d,",
            gps.date.year(), gps.date.month(), gps.date.day(), gps.time.hour(), gps.time.minute(), gps.time.second(),
            String(lat, 10).c_str(), String(lng, 10).c_str(), String(currentSpeed).c_str(),
            String(maxSpeed).c_str(), String(avgSpeed).c_str(), String(gps.course.deg()).c_str(), String(gps.altitude.feet()).c_str(), gps.satellites.value());
  }
  else
  {
    timeToLog = false;
  }

  sprintf(dofLogBuffer, "%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s",
          String(roll).c_str(), String(maxRoll).c_str(), String(avgRoll).c_str(),
          String(pitch).c_str(), String(maxPitch).c_str(), String(avgPitch).c_str(),
          String(heading).c_str(), String(bmpAltitude).c_str(),
          String(temperature).c_str(), String(maxTemperature).c_str(), String(avgTemperature).c_str(),
          String(bmp_event.pressure).c_str(), String(maxBarometricPressure).c_str(), String(avgBarometricPressure).c_str(),
          String(accel_event.acceleration.x).c_str(), String(maxAccelX).c_str(), String(avgAccelX).c_str(),
          String(accel_event.acceleration.y).c_str(), String(maxAccelY).c_str(), String(avgAccelY).c_str(),
          String(accel_event.acceleration.z).c_str(), String(maxAccelZ).c_str(), String(avgAccelZ).c_str());

  gpsBufferSize = trimwhitespace(logBuffer, 100, gpsLogBuffer);
  gpsBufferSize = trimwhitespace(logBuffer + gpsBufferSize, 150, dofLogBuffer);

  if (timeToLog) {
    if (moving) {
      radioSend(recNumber, 'C', gps.course.deg());
      radioSend(recNumber, 'A', gps.altitude.feet());
      radioSend(recNumber, 'T', temperature);
      radioSend(recNumber, 'R', maxRoll);
      radioSend(recNumber, 'P', maxPitch);
      radioSend(recNumber, 'X', maxAccelX);
      radioSend(recNumber, 'Y', maxAccelY);
      recNumber++;

      if (LOGSERIAL) {
        serialLog << logBuffer << " : " << recNumber - 1 << endl;
      }
      logfile << logBuffer << endl << flush;
      timeToLog = false;
      dataCounter = 0;
      return;
    }
    moving = false;
  }

  if (displayTimer > millis())  displayTimer = millis();
  if (millis() - displayTimer > 600000) {
    displayTimer = millis();
    char buf[50];
    if (gpsData) {

      sprintf(buf, "%02d:%02d:%02d %d", gps.time.hour(), gps.time.minute(), gps.time.second(), recNumber);
      displayStatus(buf);
    }
    else
    {
      sprintf(buf, "NO GPS %d", gps.time.hour(), gps.time.minute(), gps.time.second(), recNumber);
      displayStatus(buf);
    }
  }
  dataCounter++;
}
