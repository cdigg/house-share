'use strict';

var houseShare = angular.module('houseShare', ['ui.bootstrap'])
    //adds in unicode checkmark / x mark for boolean properties
    .filter('readOnlyCheckmark', function () {
        return function (input) {
            return input ? '\u2713' : '';//'\u2718';
        };
    })
    //converts json dates as string to actual dates
    .filter('moment', function () {
        return function (dateString, format) {
            return moment(dateString).format(format);
        }
    })
    //converts phone number to readable format
    .filter('phoneNumber', function () {
        return function (input) {
            if (input == null)
                return "";
            var ret = input;
            if (input.length == 10)
                ret = "(" + input.substr(0, 3) + ") " + input.substr(3, 3) + "-" + input.substr(6);
            else if (input.length == 7)
                ret = input.substr(0, 3) + "-" + input.substr(3);
            return ret;
        }
    });

