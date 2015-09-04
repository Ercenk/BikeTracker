(function () {
    'use strict';
    
    var app = angular.module('app', [
        // Angular modules 
        'ngAnimate',        // animations
        'ngRoute',          // routing
        'ngSanitize',       // sanitizes html bindings (ex: sidebar.js)
        'ngResource',
        'angularFileUpload',
        'ngGrid',

        // Custom modules 
        'common',           // common functions, logger, spinner
        'common.bootstrap', // bootstrap dialog wrapper functions
        'Facebook',

        // 3rd Party Modules
        'ui.bootstrap',     // ui-bootstrap (ex: carousel, pagination, dialog)
        'ui.event',
        'ui.map',
        'google-maps',
        'googlechart'
        
    ]);

    
    // Handle routing errors and success events
    app.run(['$rootScope', '$route', '$window', 'facebookSvc', appRun]);

    function appRun($rootScope, $route, $window, facebookSvc) {
        $rootScope.user = {};

        facebookSvc.watchAuthenticationStatusChange();

    };
    
})();