#include "DHT.h" //https://github.com/adafruit/DHT-sensor-library 
#include <Wire.h>
#define AirQualityAnalogPin A0
#define DHTPIN 2     // what digital pin we're connected to
#define BMP085_ADDRESS 0x77  // I2C address of BMP085
#define DHTTYPE DHT11  
#define DATA_RATE 500
const unsigned char OSS = 0;  // Oversampling Setting
int ac1,ac2,ac3;
unsigned int ac4,ac5,ac6;
int b1,b2,mb,mc,md; 
long b5; 
float pressure,pressure_filtered;
boolean flag;
char bmp085Read(unsigned char address);
int bmp085ReadInt(unsigned char address);
void bmp085Calibration();
unsigned int bmp085ReadUT();
unsigned long bmp085ReadUP();
long bmp085GetPressure(unsigned long up);
void BMPRead();
String A = "A";
String P = "P";
String T = "T";
String H = "H";
String a = "a";
String p = "p";
String t = "t";
String h = "h";
String S_pressure ;
String S_air; 
String S_temp; 
String S_humidty;
String final_data;

DHT dht(DHTPIN, DHTTYPE);
float temperature, humidity;
int AirQualityAnalog;
int BluetoothData;
int SerialData;
////////////////SETUP/////////////////
void setup() {
 Serial.begin(4800);
 Wire.begin();
 bmp085Calibration();
 dht.begin();
 pinMode(AirQualityAnalogPin,INPUT);
}
  ////////////////LOOP/////////////////
void loop() {
 BMPRead(); //Read pressure
 humidity= dht.readHumidity(); //Read humidity
 temperature = dht.readTemperature(); //Read Temprature
 AirQualityAnalog=analogRead(AirQualityAnalogPin); //Read air quality
/*****************************************************Data processing**************************************************************************************/
S_pressure  = P + String(pressure_filtered) +p;
S_air=  A + String(AirQualityAnalog)+ a; 
S_temp  =  T +String(temperature) +t; 
S_humidty  =   H + String(humidity)+h;
final_data =S_pressure + S_air + S_temp + S_humidty;
/*****************************************************DEBUGING*****************************************************************************************/
#ifdef DEBUG
Serial.println("Pressure\t Temperature\t  Humidity\t Air Quality\t");
Serial.print(pressure_filtered);
Serial.print("\t");
Serial.print(temperature);
Serial.print("\t");
Serial.print(humidity);
Serial.print("\t");
Serial.println(AirQualityAnalog);
#endif
/*****************************************************SENDING**************************************************************************************/
Serial.println(final_data); /*Send data by bluetooth*/
delay(DATA_RATE);

}
