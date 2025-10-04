
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

    });

    //load modal for editing teacher
    var teacherID;

    //loads the modal when clicked // JQUERY
    $('#EditTeacher').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        teacherID = button.data('id');

        loadEditTeacher(); //load when the modal is clicked

    });
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
    


    //immediately load the modal when the page once load
    // document.addEventListener("DOMContentLoaded", function(){
    //     loadAddTeacher();
    // })

    // JQUERY basta nag start sa dollar sign
    //Event Delegation   //Targets the form 
    $(document).on('submit', '#AddTeacherForm', function (e) {
        e.preventDefault(); // stops the default page refresh/navigation on form submission, allowing JavaScript to handle the data submission.

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
                    $('#AddTeacher').modal('hide');
                    // alert(response.message);
                    showSuccessToast(response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {
                    //$('#AddTeacherModal').html(response);

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

                    //$.each(response.errors, function (key, message) {
                    //    $('span[data-valmsg-for="' + key + '"]').text(message);
                    //});
                }
            },
            error: function (xhr, status, error) {
                console.error('Error saving teacher:', error);
                alert('Something went wrong. Please try again.');
            }
        });
    });


    var teacherID;
    //use this kase yung DeleteTeacher is yung mismong modal ko and connected sya sa button sa loob ng foreach dahil sa data-bs-target="DeleteButton"
    $('#DeleteTeacher').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget); // This is the delete pag pinindot
        teacherID = button.data('id');       // Get the ID from that button
    });

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

        $.ajax({
            url: '/Admin/Delete/' + teacherID,
            type: 'DELETE',
            success: function (response) {
                if (response.success) {
                    $('#DeleteTeacher').modal('hide');
                    showSuccessToast(response.message);
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
                showDangerToast(response);
            }
        });
    });

        
   
    function showSuccessToast(message) {
        //$('#successToast .toast-body').text(message);
        $('#toast-message').text(message);
        $('#successToast').toast('show');
    }

    function showDangerToast() {
        $('#dangerToast').toast('show');
    }
});

