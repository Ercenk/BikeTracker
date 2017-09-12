# BikeTracker
An arduino project that uses Adafruit GPS logger, 10 DOF sensor and OLED display to track any moving vehicle's location, and many other things.

## Overview
I have a long standing passion for combining data collected from around us, then analyzing and displaying using software. I have started this project to learn a few technologies.

* Hardware
    * Microcontroller (an Arduino Mega)
    * GPS receiver
    * Gyroscope
    * Accelerometer
    * Temperature sensor
    * Barometer
    * Compass
    * R/F transciever
    * MicroSD card reader
* Software
    * Hosted on Microsoft Azure Websites
    * OWIN
    * Angular
    * Various APIs on the web (time and location API from Google)
    * Google Maps
    * Facebook applications and authentication

## Here is how it works

* A new file is created every time the tracker is on.
* The data collected on the sensors and GPS are sampled every two seconds and written to the MicroSD card.
* Once done collecting the data during the day, I pop out the MicroSD card and upload using the website
* Backend code authenticates me as the administrator user
* Backend code divides the data points into segments. It is a new segment if there are more than 30 minutes between two data points.
* Each segment's start and end point is retrieved as the closest cities using a geocoder API, plus the local time zone
* Data points and segments written to the Azure table store

## Prototype

![prototype](http://ercenkbike.azurewebsites.net/Content/images/prototype.jpg)

## Testing
![Testing](http://ercenkbike.azurewebsites.net/Content/images/testing.jpg)

## Mounted on the bike
![Mounted](http://ercenkbike.azurewebsites.net/Content/images/mounted.jpg)

Site is here: http://ercenkbike.azurewebsites.net/
