(function($) {
		// captures selections used within the widget once so selectors are not-requeried
	var scrapeElements = function(context) {
			$.each([context.elements, context.inputs], function(i, set) {
				$.each(set, function(key, value) {
					set[key] = context.wrapper.find(value);
				});
			});
		},
		dstOffset = (function(){
		    var juneOffset = new Date(2012, 5, 1).getTimezoneOffset() || 0,
				januaryOffset = new Date(2012, 0, 1).getTimezoneOffset() || 0,
				offsetDifference = januaryOffset - juneOffset;
			return function(date) {
				// region with no DST
				if(offsetDifference === 0) {
					return 0;
				// region with DST
				} else {
					// positive DST offset implies northern hemisphere (compare to june), otherwise january
					var offset = offsetDifference > 0 ? juneOffset : januaryOffset;
					return Math.abs(offset === (date.getTimezoneOffset() || 0) ? offsetDifference : 0);
				}
			};
		})(),
		captureTimeZoneToCookie = function() {
			document.cookie = "tzoffset=" + escape(-1 * (new Date().getTimezoneOffset() + dstOffset(new Date()))/60);
		},
		// submits the form
		submit = function(context) {
			context.wrapper.closest('form').data('submitted',true).submit();
		},
		// sets a specific action to be submitted along with the form post
		setAction = function(context, action) {
			context.inputs.action.val(action);
		},
		// builds metadata about available auth providers based on what was rendered
		// also binds events to them for handling their usage
		setupAuthProviders = function(context) {
			// set up provider-specific actions
			context.providers = {};
			context.elements.authProviders.each(function() {
				var providerElement = $(this);

				// capture metadata about the provider
				var clientType = providerElement.attr('data-clienttype');
				context.providers[clientType] = {
					wrapper: providerElement,
					button: providerElement.find('a.submit-button')
				};

				// submit a connection request when the provider's connect button is clicked
				context.providers[clientType].button.bind('click', function(e) {
					e.preventDefault();
					// set values for the oauth connect form sbmission
					setAction(context, 'oauth_connect');
					context.inputs.provider.val(clientType);
					// trigger an event about oauth connect being requested
					$(this).trigger('oAuthConnect', [clientType]);
					// submit the form
					submit(context);
				});
			});
		},
		setupCredentialLogin = function(context) {
			context.elements.loginLink
				// add validation to the credential login
				.evolutionValidation({
					onValidated: function(isValid, buttonClicked, c) {
						if (isValid) {
							context.elements.loginLink.removeClass('disabled');
						} else {
							context.elements.loginLink.addClass('disabled');
						}
					},
					onSuccessfulClick: function(e) {
						e.preventDefault();
						// show the processing node
						$('.processing', context.elements.loginLink.parent()).css("visibility", "visible");
						// set form fields regarding the login
						context.elements.loginLink.addClass('disabled');
						// set values for the login connect form sbmission
						setAction(context, 'login');
						// trigger action that the login is occurring
						$(e.target).trigger('login');
						// submit the form
						submit(context);
					}})
				// username is required
				.evolutionValidation('addField',
					context.inputs.username,
					{ required: true, username: true, messages: {	username: context.resources.usernameInvalidUserName	}},
					context.inputs.username.closest('.field-item').find('.field-item-validation'), null)
				// password is required
				.evolutionValidation('addField',
					context.inputs.password,
					{ required: true },
					context.inputs.password.closest('.field-item').find('.field-item-validation'), null);
		},
		setupCreate = function(context) {
            if (context.mode == 'login')
            {
                context.inputs.username.change(function() {
                    var emailRegExp = new RegExp('^[!$&*\\-=^`|~#%\'\\.\"+\/?_{}\\\\a-zA-Z0-9 ]+@[\\-\\.a-zA-Z0-9]+(?:\\.[a-zA-Z0-9]+)+$');
                    if (emailRegExp.test(context.inputs.username.val()))
                       context.inputs.loginType.filter("[value=email]").prop("checked",true);
                    else
                       context.inputs.loginType.filter("[value=username]").prop("checked",true);
                });
            }

			context.elements.createAccountLink
				// add validation to the creation form
				.evolutionValidation({
					onValidated: function(isValid, buttonClicked, c) {
						if (isValid) {
							context.elements.createAccountLink.removeClass('disabled');
						} else {
							// avoid IE7/8 non-reflowing bug
							$('.join-password').css({position:'relative'});
						}
					},
					onSuccessfulClick: function(e) {
						e.preventDefault();
						// capture profile fields
						var data = context.getDynamicValues();
						// convert captured data to be prefixed with _ProfileFields_ and format dates
						var postData = {};
						$.each(data, function(key, value) {
							postData['_ProfileFields_' + key] =
								(value !== null) ?
								(value.toUTCString ? $.telligent.evolution.formatDate(value) : value + '') :
								'';
						});

						// write profile fields to hidden input
						context.inputs.profileFields.val($.param(postData));

						// show the processing node
						$('.processing', context.elements.createAccountLink.parent()).css("visibility", "visible");
						// set form fields regarding the login
						context.elements.createAccountLink.addClass('disabled');
						// trigger action that the login is occurring
						$(e.target).trigger('join');

						// submit the form
						submit(context);
					}});
			if(context.inputs.username.length > 0) {
				context.elements.createAccountLink.evolutionValidation('addField',
					context.inputs.username,
					{
						required: true,
						minlength: context.usernameMinLength,
						maxlength: context.usernameMaxLength,
						username: true,
						usernameexists: true,
						messages: {
							username: context.resources.usernameInvalidUserName,
							usernameexists: context.resources.usernameDuplicateUserName,
							required: context.resources.fieldRequired
						}
					},
					context.inputs.username.closest('.field-item').find('.field-item-validation'), null);
			}
			if(context.inputs.password.length > 0) {
				context.elements.createAccountLink.evolutionValidation('addField',
					context.inputs.password,
					{
						required: true,
						minlength: context.passwordMinLength,
						passwordvalid: true,
						messages: {
							passwordvalid: context.resources.passwordError,
							minlength: context.resources.passwordLimits,
							required: context.resources.fieldRequired
						}
					},
					context.inputs.password.closest('.field-item').find('.field-item-validation'), null);
			}
			if(context.inputs.password2.length > 0) {
				context.elements.createAccountLink.evolutionValidation('addField',
					context.inputs.password2,
					{
						required: true,
						equalTo: context.inputs.password,
						messages: {
							equalTo: context.resources.passwordMatch,
							required: context.resources.fieldRequired
						}
					},
					context.inputs.password2.closest('.field-item').find('.field-item-validation'), null);
			}

            //CAPTCHA
            if (context.inputs.captcha.length > 0 && context.captchaEnabled) {
                var captchaHidden = $(context.inputs.captcha);
                if (captchaHidden) {
                    var passed = false;
                    context.elements.createAccountLink.evolutionValidation('addCustomValidation', 'captcha_check', function () {
                        if (passed) return true; // so you only have to pass the test once
                        var resp = $.telligent.evolution.recaptcha.verify();
                        if (resp && resp.verified && resp.token) {
                            captchaHidden.val(resp.token);
                            passed = true;
                            return true;
                        }
                        return false;
                    },
                    context.resources.captchaFailure,
                    context.inputs.captcha.closest('.field-item').find('.field-item-validation'),
                     null);
                }
            }

			if(context.inputs.email.length > 0 && context.inputs.email.is(':visible')) {
				context.elements.createAccountLink.evolutionValidation('addField',
					context.inputs.email,
					{
						required: true,
						email: true,
						emailexists: true,
						messages: {
							email: context.resources.emailInvalid,
							emailexists: context.resources.emailDuplicate,
							required: context.resources.fieldRequired
						}
					},
					context.inputs.email.closest('.field-item').find('.field-item-validation'), null);
			}
			// Terms of Service Validation (when abled)
			if(context.inputs.acceptAgreement.length > 0) {
				context.elements.createAccountLink
					.evolutionValidation('addField',
						context.inputs.acceptAgreement,
						{
							required: true,
							messages: {
								required: context.resources.fieldRequired
							}
						},
						context.inputs.acceptAgreement.closest('.field-item').find('.field-item-validation'), null);
			}
			// make dynamic fields that were specified to be required required
			var prefix = context.dynamicFieldsForm + '_';
			var dynamicFields = $('[id^="'+prefix+'"]', context.wrapper);
			$.each(context.requiredDynamicfields.split(','), function(i, field) {
				if(field !== '') {
					var fieldId = field.replace(/\s/g,'');
					var dynamicField = dynamicFields.filter('[id$="'+fieldId+'"]');
					if(dynamicField !== null && dynamicField.length > 0) {
						dynamicField.parent().before('<span class="field-item-validation" style="display: none;"></span>');
						context.elements.createAccountLink
							.evolutionValidation('addCustomValidation','profileFieldHasValue',
								function(){
									return context.getHasValue(field);
								},
								context.resources.fieldRequired,
								dynamicField.closest('div').prev('.field-item-validation'), null);
						dynamicField.bind('change', function(){
							context.elements.createAccountLink.evolutionValidation('validate');
						});
					}
				}
			});
		},
		// wires up all the internal events of the plugin
		wireEvents = function(context) {
			setupAuthProviders(context);
			setupCredentialLogin(context);
			setupCreate(context);
			captureTimeZoneToCookie();

			// handle clicks to submit a collected email address
			context.elements.collectEmailLink
				.evolutionValidation({
					onValidated: function(isValid, buttonClicked, c) { },
					onSuccessfulClick: function(e) {
						e.preventDefault();
						submit(context);
					}})
				.evolutionValidation('addField',
					context.inputs.email, {
						required: true,
						email: true,
						messages: {
							required: context.resources.fieldRequired
						}
					},
					context.inputs.email.closest('.field-item').find('.field-item-validation'), null);

		},
		// wires up ui to monitor the internal plugin events to
		// affect the ui in response to events
		wireUi = function(context) {
			// set up default focus except for user creation page
            if (context.mode == 'login')
                context.inputs.username.focus();
			else {
				var input = context.wrapper.find('.page.join-manual.captcha input[type!=hidden]:first')
				if (input.length > 0)
					setTimeout(function() { input.focus(); }, 500);
				else
					context.wrapper.find('.page:not(.join-manual) input[type!=hidden]:first').focus();
			}
			// enter key on login and join inputs should attempt to login/join
			context.wrapper.bind('keypress', function(e){
				// if enter was pressed, trigger a click on the login or join link (if they exist)
				if (e.keyCode === 13) {
					e.preventDefault();
					context.elements.loginLink.click();
					context.elements.createAccountLink.click();
				}
			});
		};

	var api = {
		register: function(options) {
			var context = $.extend({}, api.defaults, options || {});
			// shallow-copy elements to avoid shared state if other widgets are rendered
			context.elements = $.extend({}, context.elements);
			// shallow-copy resources to avoid shared state if other widgets are rendered
			context.resources = $.extend({}, context.resources);

			// ensure certain elements are already jquery selections
			context.wrapper = $(context.wrapper);

			// pre-grab all the pieces of the ui so minize selections
			scrapeElements(context);
			// wire up the widget's ui to respond to its functionality
			wireUi(context);
			// wire up the widget's client functionality
			wireEvents(context);
		}
	};
	$.extend(api, {
		defaults: {
			wrapper: null, // wrapper selector
            mode: null,
			providerExpandedHeight: 247,
			providerCollapsedHeight: 18,
			animationDuration: 200,
			dynamicFieldsForm: 'form',
			// these should be overriden with actual input selectors values when registered
			inputs: {
				action: '#action',
				provider: '#provider',
				profileFields: '#profileFields',
				username: '#username',
				password: '#password',
				password2: '#password2',
				email: '#email',
				acceptAgreement: '#acceptAgreement',
				captcha: '#c_Id'
			},
			// these should be overriden with actual resource values when registered
			resources: {
				usernameInvalidUserName: 'CreateNewAccount_CreateUserStatus_InvalidUserName',
				usernameDuplicateUserName: 'CreateNewAccount_CreateUserStatus_DuplicateUsername',
				passwordError: 'CreateNewAccount_PasswordRegExValidator',
				passwordLimits: 'CreateNewAccount_PasswordLimits',
				passwordMatch: 'CreateNewAccount_PasswordNoMatch',
				emailInvalid: 'CreateNewAccount_CreateUserStatus_InvalidEmail',
				emailDuplicate: 'CreateNewAccount_CreateUserStatus_DuplicateEmailAddress',
				tosMustAgree: 'CreateNewAccount_TermsOfService_MustAgree'
            },
            captchaEnabled:false,
			// lengths should be overriden when registered
			usernameMinLength: 3,
			usernameMaxLength: 64,
			passwordMinLength: 5,
			// selectors relative to the wrapper - rarely need to be overriden
			elements: {
				loginLink: 'a.login',
				providerLinks: 'a.provider',
				credentialLinks: 'a.credential',
				privacyLinks: 'a.login-provider-privacy-link',
				collectEmailLink: 'a.collectemail',
				createAccountLink: 'a.create-account',
				authProviders: '.login-provider',
				loginProviders: 'div.login-providers',
				providerOptions: 'div.provider-options',
				loginStandardOptions: 'div.login-standard-options',
				loginStandardOptionsSelected: 'div.login-standard-options-selected',
				loginStandardFields: 'fieldset.field-list.login'
			}
		}
	});

	// expose api in a public namespace
	if (typeof $.telligent === 'undefined') { $.telligent = {}; }
	if (typeof $.telligent.evolution === 'undefined') { $.telligent.evolution = {}; }
	if (typeof $.telligent.evolution.widgets === 'undefined') { $.telligent.evolution.widgets = {}; }
	$.telligent.evolution.widgets.userLoginAndCreate = api;

})(jQuery);