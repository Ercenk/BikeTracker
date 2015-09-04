(function () {
    'use strict';

    var app = angular.module('app');

    // Collect the routes
    app.constant('routes', getRoutes());
    
    // Configure the routes and route resolvers
    app.config(['$routeProvider', 'routes', routeConfigurator]);
    function routeConfigurator($routeProvider, routes) {

        routes.forEach(function (r) {
            $routeProvider.when(r.url, r.config);
        });
        $routeProvider.otherwise({ redirectTo: '/' });
    }

    // Define the routes 
    function getRoutes() {
        return [
            {
                url: '/',
                config: {
                    templateUrl: 'app/dashboard/dashboard.html',
                    title: 'dashboard',
                    settings: {
                        content: '<i class="fa fa-dashboard"></i> Dashboard'
                    }
                }
            }, {
                url: '/upload',
                config: {
                    title: 'upload',
                    templateUrl: 'app/fileUpload/fileUpload.html',
                    settings: {
                        content: '<i class="fa fa-cloud-upload"></i> Upload'
                    }
                },

            }, {
                url: '/users',
                config: {
                    title: 'users',
                    templateUrl: 'app/users/users.html',
                    settings: {
                        content: '<i class="fa fa-cloud-upload"></i> Upload'
                    }
                },

            }
            , {
                //url: '/segment/start/:startTime/end/:endTime/startloc/:startLoc/endloc/:endLoc/segmentTime/:segmentTime',
                url: '/segment/start/:startTime/end/:endTime',
                config: {
                    title: 'upload',
                    templateUrl: 'app/segment/segment.html',
                    controller: "segmentCtrl",
                    settings: {
                        content: '<i class="fa fa-cloud-upload"></i> Upload'
                    }
                }
            }
        ];
    }
})();