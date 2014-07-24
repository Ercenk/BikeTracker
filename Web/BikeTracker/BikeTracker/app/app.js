(function () {
    'use strict';
    
    var app = angular.module('app', [
        // Angular modules 
        'ngAnimate',        // animations
        'ngRoute',          // routing
        'ngSanitize',       // sanitizes html bindings (ex: sidebar.js)
        'ngResource',

        // Custom modules 
        'common',           // common functions, logger, spinner
        'common.bootstrap', // bootstrap dialog wrapper functions
        'Facebook',

        // 3rd Party Modules
        'ui.bootstrap'      // ui-bootstrap (ex: carousel, pagination, dialog)
        
    ]);
    
    // Handle routing errors and success events
    app.run(['$rootScope', '$route', '$window', 'facebookSvc', appRun]);

    function appRun($rootScope, $route, $window, facebookSvc) {
        $rootScope.user = {};

        $window.fbAsyncInit = function() {
            FB.init({
                appId: '773946142637304',
                channelUrl: 'app/channel.html',
                status: false,
                cookie: true,
                xfbml: true,
                version: 'v2.0'
            });

            facebookSvc.watchAuthenticationStatusChange();
        };

        (function (d, s, id) {
            var js, fjs = d.getElementsByTagName(s)[0];
            if (d.getElementById(id)) return;
            js = d.createElement(s); js.id = id;
            js.src = "//connect.facebook.net/en_US/sdk.js";
            fjs.parentNode.insertBefore(js, fjs);
        }(document, 'script', 'facebook-jssdk'));
    };

})();