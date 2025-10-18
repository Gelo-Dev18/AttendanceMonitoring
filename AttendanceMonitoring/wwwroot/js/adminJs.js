
//DOM manipulation
//.addEventListener is a time of event handling
document.addEventListener("DOMContentLoaded", function () { //mag rarun to after mafully load yung HTML so dun palang gagana yung script
    //$(document).ready(function){ } // JQUERY to katumbas sya ng document.addEventListener
    //Event delegation for file input change
    $(document).on('change', 'input[type="file"].custom-file-input', function () {
        var input = this;
        //Update the custom file label with selected filename
        var fileName = input.files[0] ? input.files[0].name : 'Choose file'; //ternary operator(shorthand for if else)
        var label = $(input).next('.custom-file-label');
        label.text(fileName);

        //Display image when files is selected
        if (input.files && input.files[0]) { //0 Indicates always for true
            var reader = new FileReader();
            reader.onload = function (e) {
                //Find the image in the same modal/container
                $(input).closest('.modal-content, .container').find('#imagePreview').attr('src', e.target.result);
            }
            reader.readAsDataURL(input.files[0]);
        }
    });

    //load modal for adding teacher
    function loadAddTeacher() {
        $.ajax({ //ajax exchange data from the server without reloading the page
            url: '/Admin/AddTeacher', // Calls GET action
            type: 'GET',              // HTTP GET Request. GET is used to retrieve data from the server. POST is used for submitting data to the server
            success: function (html) {
                document.getElementById('AddTeacherModal').innerHTML = html;
            },
            error: function (xhr, status, error) {
                console.error('Error loading add teacher modal:', error);
            }
        });
    }

    //loads the modal when clicked // JQUERY
    $('#AddTeacher').on('show.bs.modal', function () {
        loadAddTeacher(); //load when the modal is clicked

        loadBlurBackground();
    });

    $('#AddTeacher').on('hide.bs.modal', function (event) {
        hideBlurBackground();
    })

    //load modal for editing teacher
    var teacherID;
    function loadEditTeacher() {
        if (!teacherID) {
            alert('Id does not found');
            return;
        }
        $.ajax({ //ajax exchange data from the server without reloading the page
            url: '/Admin/EditTeacher/' + teacherID, // Calls GET action
            type: 'GET',              // HTTP GET Request. GET is used to retrieve data from the server. POST is used for submitting data to the server
            success: function (html) {
                document.getElementById('EditTeacherModal').innerHTML = html;
            },
            error: function (xhr, status, error) {
                console.error('Error loading edit teacher modal:', error);
            }
        });
    }

    //loads the modal when clicked // JQUERY
    $('#EditTeacher').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        teacherID = button.data('id');

        loadEditTeacher();
        loadBlurBackground();


    });
    
    $('#EditTeacher').on('hide.bs.modal', function (event) {
        hideBlurBackground();
    })

    //immediately load the modal when the page once load
    // document.addEventListener("DOMContentLoaded", function(){
    //     loadAddTeacher();
    // })

    // JQUERY basta nag start sa dollar sign
    //Event Delegation   //Targets the form 
    $(document).on('submit', '#AddTeacherForm', function (e) {
        e.preventDefault(); // stops the default page refresh/navigation on form submission, allowing JavaScript to handle the data submission.

        
        //loadPageBlur();

        var formData = new FormData(this); //collect and manage form data for submission
        //RESTful api, two computer system to exchange information through the internet
        $.ajax({        
            url: '/Admin/AddTeacher',
            type: 'POST', //a RESTful API uses standard HTTP methods(GET, POST, PUT, DELETE) 
            data: formData, //sends all form data
            processData: false, // para hindi maconvert ni formdata to strings yung submission ng data lalo na if my file included
            contentType: false, //si browser mag set ng contenttype
            success: function (response) {
                //$('#AddTeacherModal').html(response); //after success it will return to Teacher List just like what's on the code in controller .  return PartialView("TeacherList");
                $('.form-control').removeClass('border-danger');
                $('.validation-error-message').text('');
                //$('#validationSum').empty();

                if (response.success) {
                    $('#AddTeacher').off('hide.bs.modal'); //Para i-disable ang blur effect for hiding modal
                    $('#AddTeacher').modal('hide');
                    // alert(response.message);
                    loadSpinner();
                    showSuccessToast(response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {
                    //$('#AddTeacherModal').html(response);

                    //used to loop over collections of elements, such as arrays, objects, or jQuery objects(collections of DOM elements).
                    //Ang pinaka purpsoe ng code na ito is para gawing border-danger yung textbox na mayroong validation
                                                     //yung KEY is yung variable or fieldname ko like FirstName, PAssowrd, email etc,
                                                          //Yung value ayun yung validation error
                    $.each(response.errors, function (key, value) {
                        //dito kapag may error is if yung value.length is > 1 is mag rarun yung code kase may error pero pag wala mag skip ito
                        // yung .length is kung ilang yung element sa loob ng array (yung error) example var errors1 = ["Email is required", "Invalid format"];
                        if (value && value.length > 0) {
                            //Dito hinahanap nyo kung anong input field base sa name attribute. Yung name attribute is ayung "asp-for" so ginagamitan sya ng key kase ayun yung fieldname
                            //then pagnahanap na is lalagyan ng border-danger yung input text box nayun kapag may error
                            var inputElement = $('[name="' + key + '"]');
                            inputElement.addClass('border-danger');
                            //Hinahanap kung may existing error message na sa baba ng input
                            var errorMessageElement = inputElement.next('.validation-error-message');
                            //so dito kapag may error or validation na, is parang irereplace nalang nya ulit yung error kahit same para tapos  ididsplay para hindi mag dagdag or patong yung html element
                            if (errorMessageElement.length > 0) {
                                //so ayun kapag may error is ididsplay and replace lng para di mag patong or dumami error sa html element
                                errorMessageElement.text(value.join(', '));
                            } else {
                                //so dito kapag nag sumbit magrarun yung else code at lalagpas yung if conditon, if wala pang existing na error then maglalagay na ng error
                                //ngayon kapag nag submit tapos same error ulit, dun na tatakbo si IF condition
                                $('<span class="text-danger validation-error-message">' + value.join(', ') + '</span>').insertAfter(inputElement);
                            }
                        }
                        
                    });

                    //$.each(response.errors, function (key, message) {
                    //    $('span[data-valmsg-for="' + key + '"]').text(message);
                    //});
                }
            },
            error: function (xhr, status, error) {
                console.error('Error saving teacher:', error);
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while updating the teacher.');
            }
        });
    });

    //For Update
    $(document).on('submit', '#EditTeacherForm', function (e) {
        e.preventDefault();

        var formData = new FormData(this);
        var teacherId = teacherID;

        loadPageBlur();

        $('#EditTeacherForm .form-control').removeClass('border-danger');
        $('#EditTeacherForm .validation-error-message').remove();

        $.ajax({
            url: '/Admin/EditTeacher/' + teacherId,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            dataType: 'json',
            success: function (response) {
                //console.log('Full response:', response); // Add this line

                if (response.success) {
                    //$('#EditTeacher').off('hide.bs.modal'); //Para i-disable ang blur effect for hiding modal
                    $('#EditTeacher').modal('hide');
                    loadSpinner();
                    showUpdateSuccessToast(response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {

                    $.each(response.errors, function (key, value) {
                        if (value && value.length > 0) {
                            var inputElement = $('[name="' + key + '"]');
                            inputElement.addClass('border-danger');

                            var errorMessageElement = inputElement.next('.validation-error-message');
                            if (errorMessageElement.length > 0) {
                                errorMessageElement.text(value.join(', '));
                            } else {
                                $('<span class="text-danger validation-error-message">' + value.join(', ') + '</span>').insertAfter(inputElement);
                            }
                        }
                    });
                    //console.log('Errors:', response.errors); // Add this line
                    //console.log('Message:', response.message); // Add this line
                //    showDangerToast(response);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error updating user:', error);
                console.log('XHR Response:', xhr.responseText); // Para makita mo yung actual error
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while updating the teacher.');
            }
        });
    });


    var teacherID;

    $('#ViewTeacher').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        teacherID = button.data('id');

        loadVIewTeacher();
        loadBlurBackground();
    });

    $('#ViewTeacher').on('hide.bs.modal', function (event) {
        hideBlurBackground();
    });
    function loadVIewTeacher() {
        if (!teacherID) {
            alert('Id does not found');
            return;
        }
        $.ajax({ //ajax exchange data from the server without reloading the page
            url: '/Admin/ViewTeacher/' + teacherID, // Calls GET action
            type: 'GET',              // HTTP GET Request. GET is used to retrieve data from the server. POST is used for submitting data to the server
            success: function (html) {
                //document.getElementById('ViewTeacherModal').innerHTML = html; //Vanilla js
                $('#ViewTeacherModal').html(html); //jquery //ito ay nasa loob ng ajax dahil nasa modal yung sarili nyang table. Wala sa mismong TeacherList, kaya di gagana if nasa loob ng datatables-demo ang assignSubject .
                $('#assignSubject').DataTable();
            },
            error: function (xhr, status, error) {
                console.error('Error loading edit teacher modal:', error);
            }
        });
    }

    //use this kase yung DeleteTeacher is yung mismong modal ko and connected sya sa button sa loob ng foreach dahil sa data-bs-target="DeleteButton"
    $('#DeleteTeacher').on('show.bs.modal', function (event) {
        loadBlurBackground();

        var button = $(event.relatedTarget); // This is the delete pag pinindot
        teacherID = button.data('id');       // Get the ID from that button
    });

    $('#DeleteTeacher').on('hide.bs.modal', function (event) {
        hideBlurBackground();
    })

    //use this kapag mismong button lang
    //$('#delete-teacher-button').on('click', function (event) {
    //    teacherID = $(this).data('id'); // 
    //});

    $('#confirmDeleteButton').on('click', function (e) {
        e.preventDefault();

        if (!teacherID) {
            alert('Id does not found');
            return;
        }

        loadSpinner();
        //loadPageBlur(); //Para kapag nag success ang isang submit like adding, deleting or editing
        
        $.ajax({
            url: '/Admin/Delete/' + teacherID,
            type: 'DELETE',
            success: function (response) {
                if (response.success) {

                    $('#DeleteTeacher').off('hide.bs.modal');// for quick fix only //Para i-disable ang blur effect for hiding modal
                    $('#DeleteTeacher').modal('hide');
                    showSuccessToast(response.message);
                    //loadSpinner();
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {
                    alert('Could not delete teacher');
                }
                //alert(response.message);
                //location.reload();
            },
            error: function (xhr, status, error) {
                console.error('Error deleting teacher:', error);
                //    alert('Something went wrong. Please try again.');
                loadSpinner();
                loadPageBlur();
                showDangerToast(response);
            }
        });
    });

        
   
    function showSuccessToast(message) {
        //$('#successToast .toast-body').text(message);
        $('#toast-message').text(message);
        $('#successToast').toast('show');
    }

    function showUpdateSuccessToast(message) {
        $('#update-toast-message').text(message);
        $('#updateSuccessToast').toast('show');
    }
    function showDangerToast() {
        $('#dangerToast').toast('show');
    }

    function loadSpinner() {
        //vanilla js
        //const spinner = document.getElementById("spinnerLoad");
        //spinner.classList.remove("d-none");

        //jquery
        $('#spinnerWrapper').removeClass("d-none");
    }

    function loadPageBlur() {
        $('#blurred-overlay-page').css('display', 'block');
    }

    function loadBlurBackground() {
        $('#blurred-overlay').css('display', 'block');
    }

    function hideBlurBackground() {
        $('#blurred-overlay').css('display', 'none');
    }

    //function loadModalBlurBackground() {
    //    $('#blurred-overlay-modal').css('display', 'block');
    //}
});

