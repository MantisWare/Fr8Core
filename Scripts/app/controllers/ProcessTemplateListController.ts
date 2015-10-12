/// <reference path="../_all.ts" />

/*
    Detail (view/add/edit) controller
*/
module dockyard.controllers {
    'use strict';

    export interface IProcessTemplateListScope extends ng.IScope {
        ExecuteProcessTemplate: (processTemplate: interfaces.IProcessTemplateVM) => void;
        GoToProcessTemplatePage: (processTemplate: interfaces.IProcessTemplateVM) => void;
        DeleteProcessTemplate: (processTemplate: interfaces.IProcessTemplateVM) => void;
        dtOptionsBuilder: any;
        dtColumnBuilder: any;
        dtInstance: any;
        activedtOptionsBuilder: any;
        activedtColumnBuilder: any;
        inactivedtOptionsBuilder: any;
        inactivedtColumnBuilder: any;   
        isFr: boolean;
        Openfr8Lines();
        OpenSettings();
    }

    /*
        List controller
    */
    class ProcessTemplateListController {
        // $inject annotation.
        // It provides $injector with information about dependencies to be injected into constructor
        // it is better to have it close to the constructor, because the parameters must match in count and type.
        // See http://docs.angularjs.org/guide/di
        public static $inject = [
            '$rootScope',
            '$scope',
            'ProcessTemplateService',
            '$modal',
            '$compile',
            '$q',
            '$http',
            'DTOptionsBuilder',
            'DTColumnBuilder',
            '$state'
        ];
        private _processTemplates: Array<interfaces.IProcessTemplateVM>;
        private _processTemplatesAc: Array<interfaces.IProcessTemplateVM>;
        private _processTemplatesIn: Array<interfaces.IProcessTemplateVM>;

        constructor(
            private $rootScope: interfaces.IAppRootScope,
            private $scope: IProcessTemplateListScope,
            private ProcessTemplateService: services.IProcessTemplateService,
            private $modal,
            private $compile: ng.ICompileService,
            private $q: ng.IQService,
            private $http: ng.IHttpService,
            private DTOptionsBuilder,
            private DTColumnBuilder,
            private $state) {

            //Clear the last result value (but still allow time for the confirmation message to show up)
            //TODO -> i really don't know why is this for??
            setTimeout(() => {
                delete $rootScope.lastResult;
            }, 300);

            $scope.$on('$viewContentLoaded', function () {
                // initialize core components
                Metronic.initAjax();
            });

            //Load Process Templates view model
           
            this._processTemplates = ProcessTemplateService.query();
            $scope.dtOptionsBuilder = this.GetDataTableOptionsFromTemplates(0);   
            $scope.dtColumnBuilder = this.GetDataTableColumns();   
            this._processTemplatesAc = ProcessTemplateService.getbystatus({ id: null, status: 2 });   
            this._processTemplatesIn = ProcessTemplateService.getbystatus({ id: null, status: 1 });
            $scope.activedtOptionsBuilder = this.GetDataTableOptionsFromTemplates(2);
            $scope.activedtColumnBuilder = this.GetDataTableColumns(); 
            $scope.inactivedtOptionsBuilder = this.GetDataTableOptionsFromTemplates(1);
            $scope.inactivedtColumnBuilder = this.GetDataTableColumns(); 
            //hold a reference to data-tables instance to be able to refresh table later
            $scope.dtInstance = {};
            $scope.ExecuteProcessTemplate = <(processTemplate: interfaces.IProcessTemplateVM) => void> angular.bind(this, this.ExecuteProcessTemplate);
            $scope.GoToProcessTemplatePage = <(processTemplate: interfaces.IProcessTemplateVM) => void> angular.bind(this, this.GoToProcessTemplatePage);
            $scope.DeleteProcessTemplate = <(processTemplate: interfaces.IProcessTemplateVM) => void> angular.bind(this, this.DeleteProcessTemplate);
            $scope.Openfr8Lines = function () {
                                $scope.isFr=true;
                            }
            
            			$scope.OpenSettings = function () {
                                $scope.isFr=false;
                           }
        }

        //this function will be called on every reloadData call to data-table
        //angular removes $promise property of _processTemplates after successful load
        //so we need to manage promises manually
        private ResolveProcessTemplatesPromise() {
            if (this._processTemplates.$promise) {
                return this._processTemplates.$promise;
            }

            return this.$q.when(this._processTemplates);
        }

        private ResolveProcessTemplatesPromiseAc() {
            if (this._processTemplatesAc.$promise) {
                return this._processTemplatesAc.$promise;
            }

            return this.$q.when(this._processTemplatesAc);
        }

        private ResolveProcessTemplatesPromiseIn() {
            if (this._processTemplatesIn.$promise) {
                return this._processTemplatesIn.$promise;
            }

            return this.$q.when(this._processTemplatesIn);
        }

        private GetDataTableOptionsFromTemplates(processtype) {
            var onRowCreate = <(row: any) => void> angular.bind(this, this.OnRowCreate);
            if (processtype == 2) {
                var resolveData = <() => void> angular.bind(this, this.ResolveProcessTemplatesPromiseAc);
            }
            else if (processtype == 1) {
                var resolveData = <() => void> angular.bind(this, this.ResolveProcessTemplatesPromiseIn);
            } else {
                var resolveData = <() => void> angular.bind(this, this.ResolveProcessTemplatesPromise);
            }
            return this.DTOptionsBuilder
                .fromFnPromise(resolveData)
                .withPaginationType('full_numbers')
                .withOption('createdRow', onRowCreate);
        }

        private OnRowCreate(row: any, data: any) {
            var ctrl = this;
            //datatables doesn't compile inserted rows. to access to scope we need to compile them
            //i think source of datatables should be changed to compile rows with it's parent scope (which is ours)
            this.$compile(angular.element(row).contents())(this.$scope);

            angular.element(row).bind('click', function () {
                ctrl.$state.go('processTemplateDetails', { id: data.id });
            });
        }

        private GetDataTableColumns() {
            return [
                this.DTColumnBuilder.newColumn('id').withTitle('Id').notVisible(),
                this.DTColumnBuilder.newColumn('name').withTitle('Name'),
                this.DTColumnBuilder.newColumn('description').withTitle('Description'),
                this.DTColumnBuilder.newColumn('processTemplateState').withTitle('Status')
                .renderWith(function(data, type, full, meta) {
                        if (data.ProcessTemplateState === 1) {
                        return '<span class="bold font-green-haze">Inactive</span>';
                    } else {
                        return '<span class="bold font-green-haze">Active</span>';
                        }

                    }),
                this.DTColumnBuilder.newColumn(null)
                    .withTitle('Actions')
                    .notSortable()
                    .renderWith(function (data: interfaces.IProcessTemplateVM, type, full, meta) {

                    var executeButton = '<button type="button" class="btn btn-sm btn-primary" ng-click="ExecuteProcessTemplate(' + data.id + ', $event);">Execute</button>';
                    var deleteButton = '<button type="button" class="btn btn-sm red" ng-click="DeleteProcessTemplate(' + data.id +', $event);">Delete</button>';
                    var editButton = '<button type="button" class="btn btn-sm green" ng-click="GoToProcessTemplatePage(' + data.id +', $event);">Edit</button>';

                    return deleteButton + editButton + executeButton;
                    })
            ];            
        }

        private ExecuteProcessTemplate(processTemplateId, $event) {
            var launchUrl = '/api/processes/launch?processTemplateId=' + processTemplateId;
            this.$http.post(launchUrl, {});

            $event.preventDefault();
            $event.stopPropagation();
        }

        private GoToProcessTemplatePage(processTemplateId, $event) {
            $event.preventDefault();
            $event.stopPropagation();
            
            this.$state.go('processBuilder', { id: processTemplateId });
        }

        private DeleteProcessTemplate(processTemplateId, $event) {
            $event.preventDefault();
            $event.stopPropagation();

            //to save closure of our controller
            var me = this;
            this.$modal.open({
                    animation: true,
                    templateUrl: 'modalDeleteConfirmation',
                    controller: 'ProcessTemplateListController__DeleteConfirmation',
 
            }).result.then(function () {
                    //Deletion confirmed
                me.ProcessTemplateService.delete({ id: processTemplateId }).$promise.then(function () {
                    me.$rootScope.lastResult = "success";
                    //now loop through our existing templates and remove from local memory
                    for (var i = 0; i < me._processTemplates.length; i++) {
                        if (me._processTemplates[i].id === processTemplateId) {
                            me._processTemplates.splice(i, 1);
                            me.$scope.dtInstance.reloadData();
                            break;
                        }
                    }
                    });
                });
        }
    }
    app.controller('ProcessTemplateListController', ProcessTemplateListController);

    /*
        A simple controller for Delete confirmation dialog.
        Note: here goes a simple (not really a TypeScript) way to define a controller. 
        Not as a class but as a lambda function.
    */
    app.controller('ProcessTemplateListController__DeleteConfirmation', ($scope: any, $modalInstance: any): void => {
        $scope.ok = () => {
            $modalInstance.close();
        };

        $scope.cancel = () => {
            $modalInstance.dismiss('cancel');
        };
    });
}