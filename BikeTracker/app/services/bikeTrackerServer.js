(function () {
    'use strict';

    var serviceId = 'bikeTrackerServer';

    angular.module('app').factory(serviceId, ['$resource', bikeTrackerServer]);

    function bikeTrackerServer($resource) {
        var authorize = $resource(
            '/api/Data/Authorize/:token',
            { token: '@token' });

        var postData = $resource(
            '/api/data/postData',
            {});

        var dataPointsUri = $resource(
            '/api/data/datapointsurl',
            {});

        var segmentsUri = $resource(
    '/api/data/segmentsurl',
    {},
    {
        get: {
            method: "GET",
            cache: true
        }
    });

        var usersUri = $resource(
    '/api/data/usersurl',
    {});

        return {
            authorize: authorize,
            postData: postData,
            dataPointsUri: dataPointsUri,
            segmentsUri: segmentsUri,
            usersUri: usersUri
        };

    }
})();